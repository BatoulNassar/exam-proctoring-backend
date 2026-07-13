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
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("AuditLog");
            builder.HasKey(al => al.id);
            builder.Property(al => al.actor_type).IsRequired().HasMaxLength(50);
            builder.Property(al => al.action).IsRequired().HasMaxLength(100);
            builder.Property(al => al.entity_type).IsRequired().HasMaxLength(50);
            builder.Property(al => al.details).HasMaxLength(1000);

            builder.HasOne(al => al.ExamSession) 
                   .WithMany(es => es.AuditLogs)
                   .HasForeignKey(al => al.exam_session_id)
                   .OnDelete(DeleteBehavior.Cascade);
            builder.ConfigureAuditFields();
        }
    }
}
