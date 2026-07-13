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
    public class MonitoringEventConfiguration : IEntityTypeConfiguration <MonitoringEvent>
    {
        public void Configure(EntityTypeBuilder<MonitoringEvent> builder)
        {
            builder.ToTable("MonitoringEvent"); 
            builder.HasKey(me => me.id);
            builder.Property(me => me.event_type).IsRequired().HasMaxLength(50);
            builder.Property(me => me.event_details).HasMaxLength(1000);

            builder.HasOne(me => me.StudentSession) 
                   .WithMany(ss => ss.MonitoringEvents)
                   .HasForeignKey(me => me.student_session_id)
                   .OnDelete(DeleteBehavior.Cascade);
            builder.ConfigureAuditFields();
        }
    }
}
