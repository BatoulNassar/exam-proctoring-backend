using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ExamProctoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExamProctoring.Infrastructure.Persistence.Configurations
{
    public class StudentSessionConfiguration :IEntityTypeConfiguration<StudentSession>
    {
        public void Configure(EntityTypeBuilder<StudentSession> builder)
        {
           builder.ToTable("StudentSession");
           builder.HasKey(ss => ss.id);
           builder.Property(ss => ss.status).IsRequired().HasMaxLength(20).HasConversion<string>();
           builder.HasIndex(ss => new { ss.exam_session_id, ss.student_id }).IsUnique();

           // ----- attempt state -----

           // Store-generated so a row enrolled by an admin gets an id without every enrollment
           // call site having to remember to set one, and so the migration can backfill
           // existing rows. EF omits the column on insert and reads the generated value back.
           builder.Property(ss => ss.public_id)
                  .IsRequired()
                  .ValueGeneratedOnAdd()
                  .HasDefaultValueSql("NEWID()");

           builder.Property(ss => ss.started_at);
           builder.Property(ss => ss.ends_at);

           // Canonical "D" UUID from the token's device_id claim, never a hardware identifier.
           builder.Property(ss => ss.device_id).IsRequired(false).HasMaxLength(36);

           builder.Property(ss => ss.question_count);

           // The student-facing attempt id must be globally unambiguous.
           builder.HasIndex(ss => ss.public_id).IsUnique();

           // ----- frozen terminal result -----

           builder.Property(ss => ss.finalised_at);
           builder.Property(ss => ss.answered_count);

           builder.Property(ss => ss.finalisation_reason)
                  .IsRequired(false)
                  .HasConversion<string>()
                  .HasMaxLength(30);

           builder.Property(ss => ss.receipt_code).IsRequired(false).HasMaxLength(64);

           // Filtered so the uniqueness applies only to issued receipts. A plain unique index
           // would permit just ONE null and therefore only one un-finalised attempt in the
           // entire table.
           builder.HasIndex(ss => ss.receipt_code)
                  .IsUnique()
                  .HasFilter("[receipt_code] IS NOT NULL");

           // Serves the auto-expiry sweep: still running, deadline passed, not yet finalised.
           builder.HasIndex(ss => new { ss.status, ss.ends_at });

           builder.HasOne(ss => ss.Student)
                   .WithMany(s => s.StudentSessions)
                   .HasForeignKey(ss => ss.student_id)
                   .OnDelete(DeleteBehavior.Restrict);

           builder.HasOne(ss => ss.ExamSession)
                   .WithMany(es => es.StudentSessions)
                   .HasForeignKey(ss => ss.exam_session_id)
                   .OnDelete(DeleteBehavior.Restrict);
            builder.ConfigureAuditFields();
        }
    }
}
