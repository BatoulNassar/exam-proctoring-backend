using ExamProctoring.Domain.Common;
using ExamProctoring.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamProctoring.Domain.Entities
{
    /// One student's assignment to an exam session AND that student's single attempt at it.
    /// The row is created by an admin at enrollment time with status NotStarted; the attempt
    /// begins when Start Exam stamps <see cref="started_at"/>. The unique index on
    /// (exam_session_id, student_id) is what enforces "one attempt per student per session".
    /// A null <see cref="started_at"/> therefore means "enrolled but never started".
     public class StudentSession : BaseEntity
    {
        public int exam_session_id { get; set; }
        public int student_id { get; set; }
        public StudentSessionStatus status { get; set; }
        public DateTime? login_at { get; set; }
        public DateTime? verified_at { get; set; }
        public bool liveness_passed { get; set; }
        public bool face_match_passed { get; set; }
        public int failed_auth_attempts { get; set; }
        public DateTime? submitted_at { get; set; }
        public int? awarded_marks { get; set; }

        /// Student-facing opaque attempt identifier. The integer primary key is never
        /// exposed in a student route, so attempt ids cannot be enumerated.
        public Guid public_id { get; set; }

        /// Server instant the attempt first started. Null until Start Exam claims it.
        /// Immutable afterwards - a resume never rewrites it.
        public DateTime? started_at { get; set; }

        /// Absolute personal deadline: started_at + ExamSession.duration_minutes, computed
        /// once at first start and stored. Never recomputed, so a later configuration edit
        /// or an admin extension cannot move a deadline the student has already been shown.
        public DateTime? ends_at { get; set; }

        /// Canonical "D" UUID of the device this attempt is bound to, taken from the
        /// authenticated token's device_id claim at first start. Null before the attempt starts.
        public string? device_id { get; set; }

        /// Number of questions materialised for this attempt, frozen at first start so the
        /// value cannot drift if the question bank is later edited.
        public int? question_count { get; set; }

        // ----- frozen terminal result -----
        // Written exactly once, by whichever path finalises the attempt first. Together these
        // make the submit receipt reproducible without ever recomputing mutable state, so a
        // retry months later still returns the same numbers.

        /// Server instant the attempt was finalised. Null while the attempt is still open.
        /// Its presence is the terminal marker, and the guard that makes finalisation a
        /// one-time operation under concurrency.
        public DateTime? finalised_at { get; set; }

        /// How the attempt ended. Drives the status the API reports.
        public AttemptFinalisationReason? finalisation_reason { get; set; }

        /// Questions with a non-empty answer at the moment of finalisation. A deliberately
        /// cleared answer keeps its row but does not count here.
        public int? answered_count { get; set; }

        /// Human-readable submission receipt, generated once at finalisation.
        /// Carries no personal data and is never accepted as an identifier or credential.
        public string? receipt_code { get; set; }

        /// Server instant auto-grading completed for this attempt, and the marker that makes
        /// grading a one-time operation.
        ///
        /// Written in the SAME transaction as the AutoScore rows, so it is never set without
        /// them and they are never left half-written without it. That is what lets a retry tell
        /// "already graded - replay the frozen snapshot" apart from "finalised but grading
        /// failed - grade it now", which is the difference between a student getting their
        /// receipt and never getting one.
        ///
        /// Deliberately separate from finalised_at: finalisation and grading are two distinct
        /// commits, and an attempt can legitimately be finalised for a moment before it is
        /// graded.
        public DateTime? graded_at_utc { get; set; }

        public Student Student { get; set; }
        public ICollection<AttemptQuestion> AttemptQuestions { get; set; } = new List<AttemptQuestion>();
        public ICollection<AutoScore> AutoScores { set; get; } = new List<AutoScore>();
        public ICollection <StudentAnswer> Answers { get; set; } = new List<StudentAnswer>();
        public ICollection <AlertEvent> Alerts { set; get; }= new List <AlertEvent>();
        public ICollection<ConnectivityBuffer> ConnectivityBuffers { get; set; } = new List<ConnectivityBuffer>();
        public ICollection<MonitoringEvent> MonitoringEvents { get; set; } = new List<MonitoringEvent>();
        public ICollection<WarningMessage> WarningMessages { get; set; } = new List<WarningMessage>();
        public ExamSession ExamSession { get; set; }
    }
}
