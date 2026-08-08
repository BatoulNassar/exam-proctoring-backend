using ExamProctoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamProctoring.Infrastructure.Persistence.Configurations
{
    public class AttemptQuestionOptionConfiguration : IEntityTypeConfiguration<AttemptQuestionOption>
    {
        public void Configure(EntityTypeBuilder<AttemptQuestionOption> builder)
        {
            builder.ToTable("AttemptQuestionOption");
            builder.HasKey(o => o.id);

            builder.Property(o => o.public_id).IsRequired();
            builder.Property(o => o.ordinal).IsRequired();

            // "a".."e" - internal only, never projected into a student-facing DTO.
            builder.Property(o => o.source_slot).IsRequired().HasMaxLength(1);

            // Snapshot of the authored option text, sized to match Question.option_a..option_e.
            builder.Property(o => o.label).IsRequired().HasMaxLength(1000);

            builder.HasOne(o => o.AttemptQuestion)
                   .WithMany(aq => aq.Options)
                   .HasForeignKey(o => o.attempt_question_id)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(o => new { o.attempt_question_id, o.public_id }).IsUnique();
            builder.HasIndex(o => new { o.attempt_question_id, o.ordinal });

            builder.ConfigureAuditFields();
        }
    }
}
