using ExamProctoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamProctoring.Infrastructure.Persistence.Configurations
{
    public class AttemptQuestionConfiguration : IEntityTypeConfiguration<AttemptQuestion>
    {
        public void Configure(EntityTypeBuilder<AttemptQuestion> builder)
        {
            builder.ToTable("AttemptQuestion");
            builder.HasKey(aq => aq.id);

            builder.Property(aq => aq.public_id).IsRequired();
            builder.Property(aq => aq.ordinal).IsRequired();

            builder.Property(aq => aq.type)
                   .IsRequired()
                   .HasConversion<string>()
                   .HasMaxLength(30);

            // Snapshot of the authored text, sized to match Question.question_text.
            builder.Property(aq => aq.stem).IsRequired().HasMaxLength(2000);
            builder.Property(aq => aq.marks).IsRequired();

            builder.HasOne(aq => aq.StudentSession)
                   .WithMany(ss => ss.AttemptQuestions)
                   .HasForeignKey(aq => aq.student_session_id)
                   .OnDelete(DeleteBehavior.Cascade);

            // Restrict mirrors StudentAnswer: an authored question that has been served to a
            // student must not be deletable out from under the attempt.
            builder.HasOne(aq => aq.Question)
                   .WithMany()
                   .HasForeignKey(aq => aq.question_id)
                   .OnDelete(DeleteBehavior.Restrict);

            // One row per question per attempt. This is what makes materialisation safe to
            // retry: a concurrent second write fails on the constraint instead of producing a
            // duplicate paper.
            builder.HasIndex(aq => new { aq.student_session_id, aq.question_id }).IsUnique();

            // Serves resolving the student-facing questionId within one attempt.
            builder.HasIndex(aq => new { aq.student_session_id, aq.public_id }).IsUnique();

            // Serves the ordered read of the whole paper.
            builder.HasIndex(aq => new { aq.student_session_id, aq.ordinal });

            builder.ConfigureAuditFields();
        }
    }
}
