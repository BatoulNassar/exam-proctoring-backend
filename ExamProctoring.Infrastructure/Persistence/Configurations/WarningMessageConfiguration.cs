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
    public class WarningMessageConfiguration : IEntityTypeConfiguration<WarningMessage>
    {
                public void Configure(EntityTypeBuilder<WarningMessage> builder)
        {
            builder.ToTable("WarningMessage");
            builder.HasKey(wm => wm.id);

            builder.Property(wm => wm.message_text).IsRequired().HasMaxLength(1000);

            builder.HasOne(wm => wm.StudentSession)
                   .WithMany(ss => ss.WarningMessages)
                   .HasForeignKey(wm => wm.student_session_id)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(wm => wm.ProctorAction)
                   .WithMany(pa => pa.WarningMessages)
                   .HasForeignKey(wm => wm.proctor_action_id)
                   .OnDelete(DeleteBehavior.Restrict);
            builder.ConfigureAuditFields();
        }
    }
}
