using ExamProctoring.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamProctoring.Domain.Entities
{
    public class Student : BaseEntity
    {
        public string user_name { get; set; }
        public string password { get; set; }
        public string email { get; set; }
        public string phone_number { get; set; }
        public string first_name { get; set; }
        public string middle_name { get; set; }
        public string last_name { get; set; }
        public string university_number { get; set; }
        public string face_id { get; set; }
        public string photo_url { get; set; }

        // ----- trusted SFace reference identity (FR-01) -----
        // Written ONLY by the administrative student import. There is no student-facing
        // enrolment path, so a student can never choose the vector they are matched against.
        // All four are nullable: every student that predates this feature stays valid, and
        // NULL reads naturally as "no trusted reference yet".

        /// 128 little-endian float32 values, L2-normalised at import so the stored form is
        /// canonical and comparison is a plain dot product. Exactly 512 bytes when present,
        /// enforced by a database check constraint.
        ///
        /// Biometric data: never returned by any API, never logged, never placed in audit text.
        public byte[]? reference_face_embedding { get; set; }

        /// Recognition model that produced the reference, e.g. "sface". A probe from a
        /// different model is not comparable and is refused rather than scored.
        public string? reference_face_model { get; set; }

        /// Model release, e.g. "2021dec". Pinned for the same reason as the model name.
        public string? reference_face_model_version { get; set; }

        /// When the external enrolment tool produced the vector. Provenance only.
        public DateTime? reference_face_generated_at { get; set; }

        public bool is_active { get; set; } = true;
        public int failed_login_attempts { get; set; }
        public DateTime? lockout_end_utc { get; set; }

        public ICollection<StudentSession> StudentSessions { get; set; } = new List<StudentSession>();
    }
}
