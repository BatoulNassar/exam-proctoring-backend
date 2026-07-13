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
    public class ConnectivityBufferConfiguration : IEntityTypeConfiguration<ConnectivityBuffer>
    {
        public void Configure(EntityTypeBuilder<ConnectivityBuffer> builder)
        {
            builder.ToTable("ConnectivityBuffer");
            builder.HasKey(cb => cb.id);
            builder.HasKey(cb => cb.id);
            builder.Property(cb => cb.buffer_type).IsRequired().HasMaxLength(50);
            builder.Property(cb => cb.action).IsRequired().HasMaxLength(50);
            builder.Property(cb => cb.encrypted_payload).IsRequired();

            builder.HasOne(cb => cb.StudentSession)
                   .WithMany(ss => ss.ConnectivityBuffers)
                   .HasForeignKey(cb => cb.student_session_id)
                   .OnDelete(DeleteBehavior.Cascade);
            builder.ConfigureAuditFields();
        }
    }
}
