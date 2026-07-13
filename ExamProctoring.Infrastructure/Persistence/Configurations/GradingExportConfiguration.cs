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
    public class GradingExportConfiguration : IEntityTypeConfiguration<GradingExport>
    {
        public void Configure(EntityTypeBuilder<GradingExport> builder)
        {
            builder.ToTable("GradingExport");
            builder.HasKey(e => e.id);

            builder.Property(ge => ge.file_path).IsRequired().HasMaxLength(500);
            builder.Property(ge => ge.format).HasConversion<string>();

            builder.HasOne(ge => ge.ExamSession)  
                   .WithMany(es => es.GradingExports)
                   .HasForeignKey(ge => ge.exam_session_id)
                   .OnDelete(DeleteBehavior.Cascade);
            builder.ConfigureAuditFields();
        }
    
    }
}
