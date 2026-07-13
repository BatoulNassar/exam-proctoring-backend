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
    public class QuestionBankConfiguration :IEntityTypeConfiguration <QuestionBank>
    {
        public void Configure(EntityTypeBuilder<QuestionBank> builder) {
            builder.ToTable("QuestionBank");
            builder.HasKey(qb => qb.id);

            builder.Property(qb=>qb.title)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(qb=>qb.course_code)
                .IsRequired()
                .HasMaxLength(10);

            builder.Property(qb=>qb.status)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(qb=>qb.version)
                .IsRequired()
                .HasMaxLength(10);

            builder.HasOne(qb => qb.Admin)
                .WithMany(a => a.QuestionBanks)
                .HasForeignKey(qb => qb.authored_by_admin_id)
                .OnDelete(DeleteBehavior.Restrict);
            builder.ConfigureAuditFields();

        } 
        }
    
    
}
