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
    public class ProctorActionConfiguration : IEntityTypeConfiguration<ProctorAction>
    {
        public void Configure(EntityTypeBuilder<ProctorAction> builder)
        {
            builder.ToTable("ProctorAction");
            builder.HasKey(pa => pa.id);

            builder.Property(pa => pa.action_type).IsRequired().HasMaxLength(50);
            builder.Property(pa => pa.action_note).HasMaxLength(500);

            builder.HasOne(pa => pa.Admin)
                   .WithMany(u => u.ProctorActions)
                   .HasForeignKey(pa => pa.admin_id)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(pa => pa.AlertEvent)
                   .WithMany(ae => ae.ProctorActions)
                   .HasForeignKey(pa => pa.alert_event_id)
                   .OnDelete(DeleteBehavior.Cascade);
            builder.ConfigureAuditFields();
        }
    }
}
