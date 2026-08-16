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
    public class AlertEventConfiguration : IEntityTypeConfiguration<AlertEvent>
    {
        public void Configure(EntityTypeBuilder<AlertEvent> builder)
        {
            builder.ToTable("AlertEvent");

            builder.HasKey(x => x.id);
            builder.Property(ae => ae.alert_type).HasMaxLength(50);
            builder.Property(ae => ae.severity).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(ae => ae.status).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(ae => ae.snapshot_url).HasMaxLength(500);

            builder.HasOne(ae => ae.StudentSession) 
                   .WithMany(ss => ss.Alerts)
                   .HasForeignKey(ae => ae.student_session_id)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ae => ae.MonitoringEvent)
                   .WithMany(me => me.AlertEvents)
                   .HasForeignKey(ae => ae.monitoring_event_id)
                   .OnDelete(DeleteBehavior.Restrict);
            builder.ConfigureAuditFields();
        }
    }
}
