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
    public class QuestionConfiguration :IEntityTypeConfiguration<Question>
    {
        public void Configure(EntityTypeBuilder<Question> builder)
        {
            builder.ToTable("Question");
            builder.HasKey(q => q.id);
            builder.Property(q => q.type).IsRequired().HasConversion<string>();
            builder.Property(q => q.question_text).IsRequired().HasMaxLength(2000);
            builder.Property(q => q.option_a).HasMaxLength(1000);
            builder.Property(q => q.option_b).HasMaxLength(1000);
            builder.Property(q => q.option_c).HasMaxLength(1000);
            builder.Property(q => q.option_d).HasMaxLength(1000);
            builder.Property(q => q.option_e).HasMaxLength(1000);
            builder.Property(q => q.correct_answer).IsRequired().HasMaxLength(255);


            builder.HasOne(q => q.QuestionBank)
                .WithMany(qb => qb.Questions)
                .HasForeignKey(q => q.question_bank_id)
                .OnDelete(DeleteBehavior.Restrict);
            builder.ConfigureAuditFields();
        }
    }
}
