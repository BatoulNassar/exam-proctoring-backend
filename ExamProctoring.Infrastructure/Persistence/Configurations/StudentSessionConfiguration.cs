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
    public class StudentSessionConfiguration :IEntityTypeConfiguration<StudentSession>
    {
        public void Configure(EntityTypeBuilder<StudentSession> builder)
        {
           builder.ToTable("StudentSession");
           builder.HasKey(ss => ss.id);
           builder.Property(ss => ss.status).IsRequired().HasMaxLength(20).HasConversion<string>();
           builder.HasIndex(ss => new { ss.exam_session_id, ss.student_id }).IsUnique();

           builder.HasOne(ss => ss.Student)
                   .WithMany(s => s.StudentSessions)
                   .HasForeignKey(ss => ss.student_id)
                   .OnDelete(DeleteBehavior.Restrict);

           builder.HasOne(ss => ss.ExamSession)
                   .WithMany(es => es.StudentSessions)
                   .HasForeignKey(ss => ss.exam_session_id)
                   .OnDelete(DeleteBehavior.Restrict);
            builder.ConfigureAuditFields();
        }
    }
}
