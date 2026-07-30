using ExamProctoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamProctoring.Infrastructure.Persistence.Configurations
{
    public class ProctorSessionConfiguration : IEntityTypeConfiguration<ProctorSession>
    {
        public void Configure(EntityTypeBuilder<ProctorSession> builder)
        {
            builder.ToTable("ProctorSession");
            builder.HasKey(ps => ps.id);

            builder.Property(ps => ps.exam_session_id).IsRequired();
            builder.Property(ps => ps.proctor_id).IsRequired();

            builder.HasOne(ps => ps.ExamSession)
                   .WithMany(es => es.ProctorSessions)
                   .HasForeignKey(ps => ps.exam_session_id)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ps => ps.Proctor)
                   .WithMany(u => u.ProctorSessions)
                   .HasForeignKey(ps => ps.proctor_id)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(ps => new { ps.exam_session_id, ps.proctor_id })
                   .IsUnique()
                   .HasName("IX_ProctorSession_Unique");

            builder.ConfigureAuditFields();
        }
    }
}
