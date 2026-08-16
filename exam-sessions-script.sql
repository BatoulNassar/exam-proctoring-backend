/*==============================================================================
  FE TEST - ELIGIBILITY  |  Seed data for GET /api/v1/sessions/eligibility
  Target student: VU-2024-002 (Layla Hassan / layla.hassan@vu.edu)
  All times: UTC (SYSUTCDATETIME())
  Safe to re-run. Creates nothing outside the 'FE TEST - ELIGIBILITY - ' prefix.

  The endpoint returns exactly ONE session, so only ONE case is observable
  at a time. Set @Case below, run, then call the API.
==============================================================================*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

-- >>> CHOOSE THE SCENARIO TO ACTIVATE <<<
DECLARE @Case varchar(20) = 'ELIGIBLE';
--  ELIGIBLE | UPCOMING | WINDOW_CLOSED | FINISHED | GRACE | SUBMITTED | IN_EXAM | MULTI_ACTIVE

DECLARE @Prefix nvarchar(40) = N'FE TEST - ELIGIBILITY - ';
DECLARE @Now    datetime2(7) = SYSUTCDATETIME();

BEGIN TRANSACTION;

/*--- 0. Case catalogue + expectations -------------------------------------*/
DECLARE @Cases TABLE (
    case_key        varchar(20)  PRIMARY KEY,
    title           nvarchar(150) NOT NULL UNIQUE,
    expect_eligible varchar(5)   NOT NULL,
    expect_reason   varchar(40)  NOT NULL);

INSERT @Cases (case_key, title, expect_eligible, expect_reason) VALUES
 ('ELIGIBLE',      @Prefix + N'Eligible Now',      'true',  '(null)'),
 ('UPCOMING',      @Prefix + N'Upcoming',          'false', 'SESSION_NOT_STARTED'),
 ('WINDOW_CLOSED', @Prefix + N'Window Closed',     'false', 'SESSION_CLOSED'),
 ('FINISHED',      @Prefix + N'Finished',          'false', 'SESSION_CLOSED'),
 ('GRACE',         @Prefix + N'Grace',             'false', 'SESSION_CLOSED'),
 ('SUBMITTED',     @Prefix + N'Submitted',         'false', 'ALREADY_SUBMITTED'),
 ('IN_EXAM',       @Prefix + N'In Exam',           'false', 'EXAM_ALREADY_STARTED'),
 ('MULTI_ACTIVE',  @Prefix + N'Second Open (409)', 'n/a',   'HTTP 409 MULTIPLE_ACTIVE_SESSIONS');

IF NOT EXISTS (SELECT 1 FROM @Cases WHERE case_key = @Case)
    THROW 50010, 'FE TEST: invalid @Case. Use ELIGIBLE, UPCOMING, WINDOW_CLOSED, FINISHED, GRACE, SUBMITTED, IN_EXAM or MULTI_ACTIVE.', 1;

/*--- 1. Resolve Layla (the Mac-app identity-verification test account) ----*/
DECLARE @StudentId int;
SELECT @StudentId = id
FROM dbo.Student
WHERE is_deleted = 0
  AND (
        user_name = N'VU-2024-002'
     OR university_number = N'VU-2024-002'
     OR email = N'layla.hassan@vu.edu'
  );

IF @StudentId IS NULL
    THROW 50011, 'FE TEST: student VU-2024-002 (Layla Hassan) not found in dbo.Student (or is soft-deleted). Import students_import.zip first. Nothing was changed.', 1;

/*--- 2. Resolve an admin User for the required FKs -------------------------*/
DECLARE @AdminId int;
SELECT TOP (1) @AdminId = id FROM dbo.[User] WHERE is_deleted = 0 ORDER BY id;

IF @AdminId IS NULL
    THROW 50012, 'FE TEST: no active row in dbo.[User]; ExamSession.created_by_admin_id / QuestionBank.authored_by_admin_id cannot be satisfied.', 1;

/*--- 3. Dedicated test QuestionBank (ExamSession.question_bank_id is NOT NULL)
        Eligibility never reads the bank or its questions - the FK just must
        resolve, so no Question rows are created.                            */
DECLARE @BankId int;
SELECT @BankId = id FROM dbo.QuestionBank WHERE title = @Prefix + N'Bank';

IF @BankId IS NULL
BEGIN
    INSERT dbo.QuestionBank
        (title, course_code, status, version, authored_by_admin_id,
         locked_at, randomization, option_shuffle, created_at, created_by, is_deleted)
    VALUES
        (@Prefix + N'Bank', N'FEELIG', N'Locked', N'1.0', @AdminId,
         @Now, 0, 0, @Now, @AdminId, 0);
    SET @BankId = SCOPE_IDENTITY();
END

/*--- 4. Create any missing ExamSessions (parked baseline) ------------------*/
INSERT dbo.ExamSession
    (title, course_tag, status, start_time, duration_minutes, question_bank_id,
     scheduled_at, active_at, locked_at, closed_at, archived_at, grace_period_ended_at,
     grace_period_minutes, extended_by_minutes, login_window_minutes,
     eye_gaze_threshold_sec, face_alert_sensitivity,
     created_by_admin_id, created_at, created_by, is_deleted)
SELECT c.title, N'FE-ELIG', N'CLOSED', DATEADD(DAY, -30, @Now), 60, @BankId,
       NULL, NULL, NULL, @Now, NULL, NULL,
       5, 0, 15, 3, N'Medium',
       @AdminId, @Now, @AdminId, 0
FROM @Cases c
WHERE NOT EXISTS (SELECT 1 FROM dbo.ExamSession e WHERE e.title = c.title);

/*--- 5. Enrol the student (respects UNIQUE (exam_session_id, student_id)) --*/
INSERT dbo.StudentSession
    (exam_session_id, student_id, status, liveness_passed, face_match_passed,
     failed_auth_attempts, created_at, created_by, is_deleted)
SELECT e.id, @StudentId, N'NotStarted', 0, 0, 0, @Now, @AdminId, 0
FROM dbo.ExamSession e
JOIN @Cases c ON c.title = e.title
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.StudentSession ss
    WHERE ss.exam_session_id = e.id AND ss.student_id = @StudentId);

/*--- 6. PARK every FE TEST session: far past + CLOSED.
        Nothing here can win Tier 1 or Tier 2, and its -30d start_time loses
        Tier 3 to whichever case we activate below. That is what makes the
        result deterministic.                                                */
UPDATE e SET
    e.status                = N'CLOSED',
    e.start_time            = DATEADD(DAY, -30, @Now),
    e.duration_minutes      = 60,
    e.login_window_minutes  = 15,
    e.extended_by_minutes   = 0,
    e.grace_period_minutes  = 5,
    e.grace_period_ended_at = NULL,
    e.scheduled_at          = NULL,
    e.locked_at             = NULL,
    e.active_at             = NULL,
    e.closed_at             = @Now,   -- recent, so CLOSED->ARCHIVED (7d) never fires
    e.archived_at           = NULL,
    e.updated_at            = @Now
FROM dbo.ExamSession e
JOIN @Cases c ON c.title = e.title;

UPDATE ss SET
    ss.status              = N'NotStarted',
    ss.login_at            = NULL,
    ss.verified_at         = NULL,
    ss.liveness_passed     = 0,
    ss.face_match_passed   = 0,
    ss.failed_auth_attempts = 0,
    ss.submitted_at        = NULL,
    ss.started_at          = NULL,
    ss.ends_at             = NULL,
    ss.device_id           = NULL,
    ss.question_count      = NULL,
    ss.finalised_at        = NULL,
    ss.finalisation_reason = NULL,
    ss.answered_count      = NULL,
    ss.receipt_code        = NULL,    -- filtered UNIQUE index: must be cleared
    ss.awarded_marks       = NULL,
    ss.is_deleted          = 0,
    ss.updated_at          = @Now
FROM dbo.StudentSession ss
JOIN dbo.ExamSession e ON e.id = ss.exam_session_id
JOIN @Cases c          ON c.title = e.title
WHERE ss.student_id = @StudentId;

/*--- 6a. Wipe identity verification so a re-seeded ELIGIBLE run can open
        the camera again. StudentSession.verified_at alone is not enough:
        POST /identity/verification-sessions keys off
        IdentityVerificationSession.verified_at_utc and returns 409
        IDV_ALREADY_VERIFIED while that stamp remains.                      */
DELETE iva
FROM dbo.IdentityVerificationAttempt iva
JOIN dbo.IdentityVerificationSession ivs ON ivs.id = iva.identity_verification_session_id
JOIN dbo.StudentSession ss ON ss.id = ivs.student_session_id
JOIN dbo.ExamSession e ON e.id = ss.exam_session_id
JOIN @Cases c ON c.title = e.title
WHERE ss.student_id = @StudentId;

DELETE ivs
FROM dbo.IdentityVerificationSession ivs
JOIN dbo.StudentSession ss ON ss.id = ivs.student_session_id
JOIN dbo.ExamSession e ON e.id = ss.exam_session_id
JOIN @Cases c ON c.title = e.title
WHERE ss.student_id = @StudentId;

/*--- 6b. Wipe prior progressive answers so a re-seeded ELIGIBLE run is a
        blank paper. Leaving StudentAnswer rows made resume hydrate every
        question as already chosen.                                         */
DELETE sa
FROM dbo.StudentAnswer sa
JOIN dbo.StudentSession ss ON ss.id = sa.student_session_id
JOIN dbo.ExamSession e ON e.id = ss.exam_session_id
JOIN @Cases c ON c.title = e.title
WHERE ss.student_id = @StudentId;

DELETE ir
FROM dbo.IdempotencyRecord ir
JOIN dbo.StudentSession ss ON ss.id = ir.student_session_id
JOIN dbo.ExamSession e ON e.id = ss.exam_session_id
JOIN @Cases c ON c.title = e.title
WHERE ss.student_id = @StudentId;

/*--- 7. Activate the chosen scenario --------------------------------------*/
DECLARE @T nvarchar(150) = (SELECT title FROM @Cases WHERE case_key = @Case);

-- ELIGIBLE: window [now-5m, now+55m); ACTIVE->CLOSED not until start+180m
IF @Case = 'ELIGIBLE'
    UPDATE dbo.ExamSession SET
        status = N'ACTIVE', start_time = DATEADD(MINUTE, -5, @Now),
        login_window_minutes = 60, duration_minutes = 120,
        active_at = DATEADD(MINUTE, -5, @Now), closed_at = NULL, updated_at = @Now
    WHERE title = @T;

-- UPCOMING: future start; SCHEDULED/LOCKED both give SESSION_NOT_STARTED
IF @Case = 'UPCOMING'
    UPDATE dbo.ExamSession SET
        status = N'SCHEDULED', start_time = DATEADD(MINUTE, 30, @Now),
        login_window_minutes = 15, duration_minutes = 60,
        scheduled_at = @Now, closed_at = NULL, updated_at = @Now
    WHERE title = @T;

-- WINDOW_CLOSED: started 3h ago, 15m window long expired
IF @Case = 'WINDOW_CLOSED'
    UPDATE dbo.ExamSession SET
        status = N'ACTIVE', start_time = DATEADD(HOUR, -3, @Now),
        login_window_minutes = 15, duration_minutes = 60,
        active_at = DATEADD(HOUR, -3, @Now), closed_at = NULL, updated_at = @Now
    WHERE title = @T;

-- FINISHED: definitively CLOSED 2 days ago (<7d so it is not auto-ARCHIVED)
IF @Case = 'FINISHED'
    UPDATE dbo.ExamSession SET
        status = N'CLOSED', start_time = DATEADD(DAY, -2, @Now),
        login_window_minutes = 15, duration_minutes = 60,
        active_at = DATEADD(DAY, -2, @Now),
        closed_at = DATEADD(MINUTE, -60, DATEADD(DAY, -2, @Now)), updated_at = @Now
    WHERE title = @T;

-- GRACE: hard-closed for NEW starts -> SESSION_CLOSED, session.status "GRACE"
IF @Case = 'GRACE'
    UPDATE dbo.ExamSession SET
        status = N'GRACE', start_time = DATEADD(MINUTE, -90, @Now),
        login_window_minutes = 15, duration_minutes = 60,
        extended_by_minutes = 30, grace_period_minutes = 30,
        grace_period_ended_at = DATEADD(HOUR, 6, @Now),   -- long buffer vs the janitor
        active_at = DATEADD(MINUTE, -90, @Now), closed_at = NULL, updated_at = @Now
    WHERE title = @T;

-- SUBMITTED: attempt state outranks timing, so the reason is stable
IF @Case = 'SUBMITTED'
BEGIN
    UPDATE dbo.ExamSession SET
        status = N'ACTIVE', start_time = DATEADD(HOUR, -2, @Now),
        login_window_minutes = 15, duration_minutes = 60,
        active_at = DATEADD(HOUR, -2, @Now), closed_at = NULL, updated_at = @Now
    WHERE title = @T;

    UPDATE ss SET
        ss.status       = N'Submitted',
        ss.login_at     = DATEADD(MINUTE, -118, @Now),
        ss.verified_at  = DATEADD(MINUTE, -117, @Now),
        ss.submitted_at = DATEADD(MINUTE, -70,  @Now),
        ss.updated_at   = @Now
    FROM dbo.StudentSession ss
    JOIN dbo.ExamSession e ON e.id = ss.exam_session_id
    WHERE e.title = @T AND ss.student_id = @StudentId;
END

-- IN_EXAM: long duration keeps ExamSession ACTIVE, otherwise the janitor
-- would flip it to CLOSED and the reason would become SESSION_CLOSED.
IF @Case = 'IN_EXAM'
BEGIN
    UPDATE dbo.ExamSession SET
        status = N'ACTIVE', start_time = DATEADD(HOUR, -3, @Now),
        login_window_minutes = 15, duration_minutes = 480,
        active_at = DATEADD(HOUR, -3, @Now), closed_at = NULL, updated_at = @Now
    WHERE title = @T;

    UPDATE ss SET
        ss.status      = N'InExam',
        ss.login_at    = DATEADD(MINUTE, -175, @Now),
        ss.verified_at = DATEADD(MINUTE, -174, @Now),
        ss.updated_at  = @Now
    FROM dbo.StudentSession ss
    JOIN dbo.ExamSession e ON e.id = ss.exam_session_id
    WHERE e.title = @T AND ss.student_id = @StudentId;
END

-- MULTI_ACTIVE: two open windows -> the 409 conflict path
IF @Case = 'MULTI_ACTIVE'
    UPDATE dbo.ExamSession SET
        status = N'ACTIVE', start_time = DATEADD(MINUTE, -5, @Now),
        login_window_minutes = 60, duration_minutes = 120,
        active_at = DATEADD(MINUTE, -5, @Now), closed_at = NULL, updated_at = @Now
    WHERE title IN (@Prefix + N'Eligible Now', @Prefix + N'Second Open (409)');

COMMIT TRANSACTION;

/*==============================  RESULTS  ==================================*/
SELECT
    c.case_key,
    CASE WHEN c.case_key = @Case
              OR (@Case = 'MULTI_ACTIVE' AND c.case_key = 'ELIGIBLE')
         THEN '<<< ACTIVE' ELSE '' END              AS active_case,
    e.id                                            AS exam_session_id,
    e.title,
    e.status                                        AS exam_session_status,
    e.start_time                                    AS start_time_utc,
    e.duration_minutes, e.login_window_minutes,
    e.extended_by_minutes, e.grace_period_minutes,
    DATEADD(MINUTE, e.login_window_minutes, e.start_time)                        AS login_window_closes_utc,
    DATEADD(MINUTE, e.duration_minutes + e.extended_by_minutes, e.start_time)    AS end_time_utc,
    ss.id                                           AS student_session_id,
    ss.status                                       AS student_session_status,
    ss.submitted_at                                 AS submitted_at_utc,
    s.user_name                                     AS student_username,
    c.expect_eligible                               AS expected_is_eligible,
    c.expect_reason                                 AS expected_reason_code
FROM @Cases c
JOIN dbo.ExamSession   e  ON e.title = c.title
JOIN dbo.StudentSession ss ON ss.exam_session_id = e.id AND ss.student_id = @StudentId
JOIN dbo.Student        s  ON s.id = @StudentId
ORDER BY CASE WHEN c.case_key = @Case THEN 0 ELSE 1 END, c.case_key;

/*--- Ambiguity check: EVERY visible assignment, test and real.
      window_open uses the exact IsStartWindowOpen formula.
      If more than one row shows window_open = 1, the API returns HTTP 409.  */
SELECT
    e.id AS exam_session_id, e.title, e.status AS exam_status,
    e.start_time AS start_time_utc, ss.status AS student_status,
    CASE WHEN e.title LIKE @Prefix + N'%' THEN 'FE TEST' ELSE 'REAL DATA' END AS origin,
    CASE WHEN ss.status <> N'Terminated'
              AND ss.status <> N'Submitted' AND ss.submitted_at IS NULL
              AND e.status NOT IN (N'GRACE', N'CLOSED', N'ARCHIVED')
              AND @Now >= e.start_time
              AND @Now <  DATEADD(MINUTE, e.login_window_minutes, e.start_time)
              AND @Now <  DATEADD(MINUTE, e.duration_minutes + e.extended_by_minutes, e.start_time)
         THEN 1 ELSE 0 END AS window_open
FROM dbo.StudentSession ss
JOIN dbo.ExamSession e ON e.id = ss.exam_session_id
WHERE ss.student_id = @StudentId
  AND ss.is_deleted = 0 AND e.is_deleted = 0
  AND e.status <> N'DRAFT'
ORDER BY window_open DESC, e.start_time DESC;