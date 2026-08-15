using ExamProctoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamProctoring.Infrastructure.Persistence.Configurations
{
    public class IdentityVerificationAttemptConfiguration
        : IEntityTypeConfiguration<IdentityVerificationAttempt>
    {
        public void Configure(EntityTypeBuilder<IdentityVerificationAttempt> builder)
        {
            builder.ToTable("IdentityVerificationAttempt");
            builder.HasKey(a => a.id);

            builder.Property(a => a.client_attempt_id).IsRequired().HasMaxLength(100);

            builder.Property(a => a.outcome)
                   .IsRequired()
                   .HasConversion<string>()
                   .HasMaxLength(30);

            builder.Property(a => a.attempt_number).IsRequired();
            builder.Property(a => a.attempts_remaining_after).IsRequired();

            builder.Property(a => a.match_score);
            builder.Property(a => a.threshold_used);

            builder.Property(a => a.liveness_accepted).IsRequired();
            builder.Property(a => a.liveness_blink_count).IsRequired();
            builder.Property(a => a.liveness_frames_analysed).IsRequired();
            builder.Property(a => a.liveness_duration_ms).IsRequired();
            builder.Property(a => a.liveness_min_eye_openness).IsRequired();
            builder.Property(a => a.liveness_max_eye_openness).IsRequired();
            builder.Property(a => a.liveness_rejection_reason).IsRequired(false).HasMaxLength(200);

            builder.Property(a => a.embedding_model).IsRequired().HasMaxLength(50);
            builder.Property(a => a.embedding_model_version).IsRequired().HasMaxLength(50);

            builder.Property(a => a.captured_at_utc);
            builder.Property(a => a.attempted_at_utc).IsRequired();

            // The idempotency control. A retry after a lost response hits this index instead
            // of consuming a second attempt, which is the whole point of clientAttemptId.
            builder.HasIndex(a => new { a.identity_verification_session_id, a.client_attempt_id })
                   .IsUnique();

            // Serves the audit read: every attempt for one verification, in order.
            builder.HasIndex(a => new { a.identity_verification_session_id, a.attempted_at_utc });

            builder.HasOne(a => a.IdentityVerificationSession)
                   .WithMany(s => s.Attempts)
                   .HasForeignKey(a => a.identity_verification_session_id)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.ConfigureAuditFields();
        }
    }
}
