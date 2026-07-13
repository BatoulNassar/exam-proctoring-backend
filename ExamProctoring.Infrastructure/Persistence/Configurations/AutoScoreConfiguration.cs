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
    public class AutoScoreConfiguration :IEntityTypeConfiguration<AutoScore>
    {
        public void Configure(EntityTypeBuilder<AutoScore> builder)
        {
            builder.ToTable("AutoScore");

            builder.HasKey(x => x.id);
            builder.Property(asCore => asCore.student_answer).HasMaxLength(10);
            builder.Property(asCore => asCore.correct_answer).HasMaxLength(10);

            builder.HasOne(asCore => asCore.StudentSession) 
                   .WithMany(ss => ss.AutoScores)
                   .HasForeignKey(asCore => asCore.student_session_id)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(asCore => asCore.Question)
                   .WithMany(q => q.AutoScores)
                   .HasForeignKey(asCore => asCore.question_id)
                   .OnDelete(DeleteBehavior.Restrict);
            builder.ConfigureAuditFields();
        }
        }
}
