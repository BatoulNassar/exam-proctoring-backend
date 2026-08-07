using ExamProctoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamProctoring.Infrastructure.Persistence.Configurations
{
    public class DeviceCheckRequirementConfiguration : IEntityTypeConfiguration<DeviceCheckRequirement>
    {
        public void Configure(EntityTypeBuilder<DeviceCheckRequirement> builder)
        {
            builder.ToTable("DeviceCheckRequirement");
            builder.HasKey(dcr => dcr.id);

            builder.Property(dcr => dcr.requirement_id).IsRequired().HasMaxLength(50);
            builder.Property(dcr => dcr.status)
                   .IsRequired()
                   .HasConversion<string>()
                   .HasMaxLength(10);
            builder.Property(dcr => dcr.detail).IsRequired(false).HasMaxLength(200);

            builder.HasOne(dcr => dcr.DeviceCheck)
                   .WithMany(dc => dc.Requirements)
                   .HasForeignKey(dcr => dcr.device_check_id)
                   .OnDelete(DeleteBehavior.Cascade);

            // Serves "same requirement failing across many machines" reporting.
            builder.HasIndex(dcr => new { dcr.requirement_id, dcr.status });

            builder.ConfigureAuditFields();
        }
    }
}
