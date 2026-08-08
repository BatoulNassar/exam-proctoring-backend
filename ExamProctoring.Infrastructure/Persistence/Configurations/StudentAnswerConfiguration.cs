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

         // The upsert key: exactly one current answer per question per attempt. Also the
         // constraint that makes a concurrent first-insert race safe to recover from.
         builder.HasIndex(sa => new { sa.student_session_id, sa.question_id })
                .IsUnique();

         // Deliberately left as nvarchar(max). SQL Server caps nvarchar(n) at 4000 characters,
         // so a bound large enough for the largest legitimate document - a 4000-character ESSAY
         // plus canonical JSON wrapping, plus JSON escaping which can expand that text further -
         // is not expressible as a fixed nvarchar length. Setting HasMaxLength here would record
         // a model-level limit that produces no DDL and that the repository's raw upsert does
         // not enforce, i.e. a constraint in name only.
         //
         // The real bound is applied where it can be reported properly: the service rejects text
         // over the per-type contract limit (500 / 4000) with VALIDATION_FAILED before encoding.
         builder.Property(sa => sa.student_response).IsRequired();
         builder.Property(sa => sa.saved_at).IsRequired();
         builder.Property(sa => sa.duration_ms).IsRequired().HasDefaultValue(0);
         builder.Property(sa => sa.client_answered_at).IsRequired(false);
      
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
