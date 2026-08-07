using ExamProctoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamProctoring.Infrastructure.Persistence.Configurations
{
    public class SystemSettingsConfiguration : IEntityTypeConfiguration<SystemSettings>
    {
        public void Configure(EntityTypeBuilder<SystemSettings> builder)
        {
            builder.ToTable("SystemSettings");
            builder.HasKey(s => s.id);
            builder.Property(s => s.face_sensitivity).HasConversion<string>().HasMaxLength(10).IsRequired();
            builder.ConfigureAuditFields();
        }
    }
}
