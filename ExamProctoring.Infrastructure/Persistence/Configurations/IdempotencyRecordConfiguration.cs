using ExamProctoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamProctoring.Infrastructure.Persistence.Configurations
{
    public class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
    {
        public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
        {
            builder.ToTable("IdempotencyRecord");
            builder.HasKey(r => r.id);

            builder.Property(r => r.idempotency_key).IsRequired().HasMaxLength(64);
            builder.Property(r => r.endpoint).IsRequired().HasMaxLength(64);
            builder.Property(r => r.resource_key).IsRequired().HasMaxLength(64);

            // SHA-256 is always 32 bytes; fixed width keeps the comparison cheap.
            builder.Property(r => r.request_hash).IsRequired().HasColumnType("varbinary(32)");

            builder.Property(r => r.response_status).IsRequired();
            builder.Property(r => r.response_body).IsRequired().HasMaxLength(4000);
            builder.Property(r => r.created_at_utc).IsRequired();

            // Cascade with the attempt: once the attempt is gone the keys can never be replayed.
            builder.HasOne<StudentSession>()
                   .WithMany()
                   .HasForeignKey(r => r.student_session_id)
                   .OnDelete(DeleteBehavior.Cascade);

            // One key per attempt. This is the concurrency control for the whole feature: two
            // simultaneous retries cannot both insert, so exactly one applies the mutation and
            // the loser is routed to replay.
            builder.HasIndex(r => new { r.student_session_id, r.idempotency_key }).IsUnique();

            builder.ConfigureAuditFields();
        }
    }
}
