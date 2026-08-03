using ExamProctoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamProctoring.Infrastructure.Persistence.Configurations
{
    public class StudentLoginAttemptConfiguration : IEntityTypeConfiguration<StudentLoginAttempt>
    {
        public void Configure(EntityTypeBuilder<StudentLoginAttempt> builder)
        {
            builder.ToTable("StudentLoginAttempt");
            builder.HasKey(la => la.id);

            // HMAC-SHA256 rendered as lowercase hex.
            builder.Property(la => la.identifier_hash).IsRequired().HasMaxLength(64);
            builder.Property(la => la.failed_attempts).IsRequired().HasDefaultValue(0);
            builder.Property(la => la.lockout_end_utc).IsRequired(false);

            builder.HasIndex(la => la.identifier_hash).IsUnique();
            builder.ConfigureAuditFields();
        }
    }
}
