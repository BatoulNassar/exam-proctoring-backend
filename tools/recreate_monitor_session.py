#!/usr/bin/env python3
"""Recreate an ACTIVE exam window for VU-2024-002 with verified_at + blank paper."""
from __future__ import annotations

import sys
from datetime import datetime, timedelta, timezone

import pymssql

SERVER = r"manaraljarkas.visual-host.com\MSSQLSERVER2022"
DATABASE = "exam_proctoring_d"
USER = "examuser"
PASSWORD = "ExamProctoring123"
STUDENT_KEYS = ("VU-2024-002", "layla.hassan@vu.edu")


def main() -> int:
    conn = pymssql.connect(
        server=SERVER,
        user=USER,
        password=PASSWORD,
        database=DATABASE,
        login_timeout=30,
        timeout=60,
    )
    cur = conn.cursor(as_dict=True)
    now = datetime.now(timezone.utc).replace(tzinfo=None)

    cur.execute(
        """
        SELECT TOP 1 id, user_name, university_number, email
        FROM examuser.Student
        WHERE is_deleted = 0
          AND (
            user_name IN (%s, %s)
            OR university_number IN (%s, %s)
            OR email IN (%s, %s)
          )
        """,
        (*STUDENT_KEYS, *STUDENT_KEYS, *STUDENT_KEYS),
    )
    student = cur.fetchone()
    if not student:
        print("ERROR: student VU-2024-002 not found", file=sys.stderr)
        return 1
    student_id = student["id"]
    print(f"Student id={student_id} user={student['user_name']}")

    # Prefer a session that already has materialisable questions for this student,
    # else any ACTIVE/recent session with a locked bank that has questions.
    cur.execute(
        """
        SELECT TOP 1
            e.id AS exam_session_id,
            e.title,
            e.question_bank_id,
            ss.id AS student_session_id,
            ss.status AS student_status,
            (
                SELECT COUNT(1)
                FROM examuser.Question q
                WHERE q.question_bank_id = e.question_bank_id
                  AND q.is_deleted = 0
            ) AS question_count
        FROM examuser.ExamSession e
        JOIN examuser.StudentSession ss
          ON ss.exam_session_id = e.id AND ss.student_id = %s AND ss.is_deleted = 0
        WHERE e.is_deleted = 0
        ORDER BY
            CASE WHEN e.id = 67 THEN 0 ELSE 1 END,
            question_count DESC,
            e.id DESC
        """,
        (student_id,),
    )
    row = cur.fetchone()
    if not row:
        print("ERROR: no StudentSession for Layla", file=sys.stderr)
        return 1

    exam_id = row["exam_session_id"]
    ss_id = row["student_session_id"]
    qcount = row["question_count"]
    print(
        f"Using exam_session_id={exam_id} title={row['title']!r} "
        f"student_session_id={ss_id} questions_in_bank={qcount}"
    )
    if qcount < 1:
        print("ERROR: chosen bank has no questions", file=sys.stderr)
        return 1

    start = now - timedelta(minutes=5)
    cur.execute(
        """
        UPDATE examuser.ExamSession SET
            status = N'ACTIVE',
            start_time = %s,
            login_window_minutes = 120,
            duration_minutes = 120,
            extended_by_minutes = 0,
            grace_period_minutes = 5,
            grace_period_ended_at = NULL,
            scheduled_at = NULL,
            locked_at = NULL,
            active_at = %s,
            closed_at = NULL,
            archived_at = NULL,
            updated_at = %s
        WHERE id = %s
        """,
        (start, start, now, exam_id),
    )

    # Wipe prior attempt artefacts so Monitor can start clean.
    cur.execute(
        """
        DELETE sa FROM examuser.StudentAnswer sa
        WHERE sa.student_session_id = %s
        """,
        (ss_id,),
    )
    cur.execute(
        """
        DELETE ir FROM examuser.IdempotencyRecord ir
        WHERE ir.student_session_id = %s
        """,
        (ss_id,),
    )
    cur.execute(
        """
        DELETE iva
        FROM examuser.IdentityVerificationAttempt iva
        JOIN examuser.IdentityVerificationSession ivs
          ON ivs.id = iva.identity_verification_session_id
        WHERE ivs.student_session_id = %s
        """,
        (ss_id,),
    )
    cur.execute(
        """
        DELETE FROM examuser.IdentityVerificationSession
        WHERE student_session_id = %s
        """,
        (ss_id,),
    )
    # Optional monitor leftovers (ignore if tables differ)
    for sql in (
        "DELETE FROM examuser.MonitoringEvent WHERE student_session_id = %s",
        "DELETE FROM examuser.AlertEvent WHERE student_session_id = %s",
        "DELETE FROM examuser.WarningMessage WHERE student_session_id = %s",
    ):
        try:
            cur.execute(sql, (ss_id,))
        except Exception as exc:  # noqa: BLE001
            print(f"skip cleanup: {exc}")

    cur.execute(
        """
        UPDATE examuser.StudentSession SET
            status = N'NotStarted',
            login_at = NULL,
            verified_at = %s,
            liveness_passed = 1,
            face_match_passed = 1,
            failed_auth_attempts = 0,
            submitted_at = NULL,
            started_at = NULL,
            ends_at = NULL,
            device_id = NULL,
            question_count = NULL,
            finalised_at = NULL,
            finalisation_reason = NULL,
            answered_count = NULL,
            receipt_code = NULL,
            awarded_marks = NULL,
            is_deleted = 0,
            updated_at = %s
        WHERE id = %s
        """,
        (now, now, ss_id),
    )

    # Ensure at least one proctor is assigned so Live Monitoring can open the session.
    cur.execute(
        """
        SELECT TOP 1 u.id
        FROM examuser.[User] u
        JOIN examuser.User_Roles ur ON ur.user_id = u.id
        JOIN examuser.Role r ON r.id = ur.role_id
        WHERE u.is_deleted = 0 AND r.name IN (N'Proctor', N'Admin', N'SuperAdmin')
        ORDER BY CASE r.name
            WHEN N'Proctor' THEN 0
            WHEN N'Admin' THEN 1
            ELSE 2 END, u.id
        """
    )
    proctor = cur.fetchone()
    if proctor:
        cur.execute(
            """
            IF NOT EXISTS (
                SELECT 1 FROM examuser.ProctorSession
                WHERE exam_session_id = %s AND proctor_id = %s AND is_deleted = 0
            )
            INSERT examuser.ProctorSession
                (exam_session_id, proctor_id, created_at, created_by, is_deleted)
            VALUES (%s, %s, %s, %s, 0)
            """,
            (
                exam_id,
                proctor["id"],
                exam_id,
                proctor["id"],
                now,
                proctor["id"],
            ),
        )
        print(f"Proctor assigned user_id={proctor['id']}")

    conn.commit()
    print("OK — session recreated for monitor smoke")
    print(f"  examSessionId={exam_id}")
    print(f"  studentSessionId={ss_id}")
    print("  student login: VU-2024-002 / Student123!")
    print("  dashboard: admin@exam.com / Admin123! (or seeded proctor / Proctor123!)")
    conn.close()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
