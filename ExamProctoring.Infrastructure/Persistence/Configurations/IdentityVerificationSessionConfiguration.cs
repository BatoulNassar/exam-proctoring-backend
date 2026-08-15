using ExamProctoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamProctoring.Infrastructure.Persistence.Configurations
{
    public class IdentityVerificationSessionConfiguration
        : IEntityTypeConfiguration<IdentityVerificationSession>
    {
        public void Configure(EntityTypeBuilder<IdentityVerificationSession> builder)
        {
            builder.ToTable("IdentityVerificationSession");
            builder.HasKey(s => s.id);

            // Store-generated, matching StudentSession.public_id: the caller never has to
            // remember to set one, and EF reads the generated value back after insert.
            builder.Property(s => s.public_id)
                   .IsRequired()
                   .ValueGeneratedOnAdd()
                   .HasDefaultValueSql("NEWID()");

            builder.Property(s => s.status)
                   .IsRequired()
                   .HasConversion<string>()
                   .HasMaxLength(20);

            builder.Property(s => s.attempts_used).IsRequired();
            builder.Property(s => s.max_attempts).IsRequired();
            builder.Property(s => s.verified_at_utc);

            builder.Property(s => s.device_id).IsRequired(false).HasMaxLength(36);

            // The student-facing id must be globally unambiguous.
            builder.HasIndex(s => s.public_id).IsUnique();

            // One verification per exam assignment. This is what makes "create or resume"
            // exact rather than best-effort: a concurrent second create cannot succeed, so
            // nobody can obtain a fresh attempt budget by opening the client twice.
            builder.HasIndex(s => s.student_session_id).IsUnique();

            builder.HasOne(s => s.StudentSession)
                   .WithMany()
                   .HasForeignKey(s => s.student_session_id)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.ConfigureAuditFields();
        }
    }
}
