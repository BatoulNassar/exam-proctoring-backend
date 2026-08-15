using ExamProctoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamProctoring.Infrastructure.Persistence.Configurations
{
    public class StudentConfiguration : IEntityTypeConfiguration<Student>
    {
        public void Configure(EntityTypeBuilder<Student> builder)
        {
            // 128 * sizeof(float32). The check makes a truncated or padded reference vector
            // unstorable rather than letting it decode to garbage that would score against
            // every student alike.
            builder.ToTable("Student", t => t.HasCheckConstraint(
                "CK_Student_reference_face_embedding_length",
                "[reference_face_embedding] IS NULL OR DATALENGTH([reference_face_embedding]) = 512"));

            builder.HasKey(s => s.id);
            builder.Property(s => s.user_name).IsRequired().HasMaxLength(50);
            builder.Property(s => s.password).IsRequired().HasMaxLength(255);
            builder.Property(s => s.email).IsRequired().HasMaxLength(100);
            builder.Property(s => s.phone_number).IsRequired().HasMaxLength(20);
            builder.Property(s=>s.first_name).IsRequired().HasMaxLength(50);
            builder.Property(s => s.middle_name).IsRequired(false).HasMaxLength(50);
            builder.Property(s=>s.last_name).IsRequired().HasMaxLength(50);
            builder.Property(s=>s.university_number).IsRequired().HasMaxLength(20);
            builder.Property(s => s.face_id).IsRequired(false).HasMaxLength(500);
            builder.Property(s => s.photo_url).IsRequired().HasMaxLength(500);

            // ----- trusted SFace reference identity (FR-01) -----
            // All nullable so every existing student row stays valid: NULL means "no trusted
            // reference yet", which is exactly the state before the enrolment backfill runs.
            builder.Property(s => s.reference_face_embedding)
                   .IsRequired(false)
                   .HasColumnType("varbinary(512)");

            builder.Property(s => s.reference_face_model).IsRequired(false).HasMaxLength(50);
            builder.Property(s => s.reference_face_model_version).IsRequired(false).HasMaxLength(50);
            builder.Property(s => s.reference_face_generated_at).IsRequired(false);

            // Student desktop authentication state. Defaults keep existing and newly
            // imported students active with a clean counter.
            builder.Property(s => s.is_active).IsRequired().HasDefaultValue(true);
            builder.Property(s => s.failed_login_attempts).IsRequired().HasDefaultValue(0);
            builder.Property(s => s.lockout_end_utc).IsRequired(false);


            builder.HasIndex(s => s.user_name).IsUnique();
            builder.HasIndex(s => s.email).IsUnique();
            builder.HasIndex(s => s.university_number).IsUnique();
            builder.ConfigureAuditFields();
        }
    }
}
