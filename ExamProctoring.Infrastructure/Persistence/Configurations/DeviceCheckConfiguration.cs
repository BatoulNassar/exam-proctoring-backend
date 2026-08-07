using ExamProctoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamProctoring.Infrastructure.Persistence.Configurations
{
    public class DeviceCheckConfiguration : IEntityTypeConfiguration<DeviceCheck>
    {
        public void Configure(EntityTypeBuilder<DeviceCheck> builder)
        {
            builder.ToTable("DeviceCheck");
            builder.HasKey(dc => dc.id);

            builder.Property(dc => dc.device_id).IsRequired().HasMaxLength(36);
            builder.Property(dc => dc.checked_at_utc).IsRequired();
            builder.Property(dc => dc.received_at_utc).IsRequired();
            builder.Property(dc => dc.client_can_proceed).IsRequired();
            builder.Property(dc => dc.exam_session_status)
                   .IsRequired()
                   .HasConversion<string>()
                   .HasMaxLength(20);

            // No navigation is added to StudentSession, so that entity stays untouched.
            builder.HasOne(dc => dc.StudentSession)
                   .WithMany()
                   .HasForeignKey(dc => dc.student_session_id)
                   .OnDelete(DeleteBehavior.Cascade);

            // Serves "history for a student session" and "latest attempt" reads.
            builder.HasIndex(dc => new { dc.student_session_id, dc.received_at_utc });

            builder.ConfigureAuditFields();
        }
    }
}
