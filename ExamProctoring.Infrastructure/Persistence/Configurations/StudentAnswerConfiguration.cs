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
    public class StudentAnswerConfiguration : IEntityTypeConfiguration<StudentAnswer>
    {
        public void Configure(EntityTypeBuilder<StudentAnswer> builder)
    {
         builder.ToTable("StudentAnswer");

         builder.HasKey(sa => sa.id);
         builder.HasIndex(sa => new { sa.student_session_id, sa.question_id })
                .IsUnique();
      
         builder.HasOne(sa => sa.StudentSession)
                   .WithMany(ss => ss.Answers)
                   .HasForeignKey(sa => sa.student_session_id)
                   .OnDelete(DeleteBehavior.Cascade);

         builder.HasOne(sa => sa.Question)
                   .WithMany(q => q.StudentAnswers)
                   .HasForeignKey(sa => sa.question_id)
                   .OnDelete(DeleteBehavior.Restrict);
            builder.ConfigureAuditFields();
        }
    }
}
