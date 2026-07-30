using ExamProctoring.Domain.Entities;
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
    public class ExamSessionConfiguration: IEntityTypeConfiguration<ExamSession>
    {
        public void Configure(EntityTypeBuilder<ExamSession> builder)
        {
            builder.ToTable("ExamSession");
            builder.HasKey(e => e.id);

            builder.Property(es => es.title).IsRequired().HasMaxLength(150);
            builder.Property(es => es.course_tag).IsRequired().HasMaxLength(50);
            builder.Property(es => es.status).IsRequired().HasConversion<string>();
            builder.Property(es => es.face_alert_sensitivity)
                   .IsRequired()
                   .HasConversion<string>()
                   .HasMaxLength(10);

            builder.HasOne(es => es.CreatedByAdmin)
                   .WithMany(u => u.ExamSessions)
                   .HasForeignKey(es => es.created_by_admin_id)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(es => es.QuestionBank)
                   .WithMany(qb => qb.ExamSessions) 
                   .HasForeignKey(es => es.question_bank_id)
                   .OnDelete(DeleteBehavior.Restrict);
            builder.ConfigureAuditFields();
        }
    }
}
