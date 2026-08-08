using ExamProctoring.Application.Common;
using ExamProctoring.Application.Features.ExamAttempts.DTOs;
using FluentValidation;

namespace ExamProctoring.Application.Features.ExamAttempts.Validators
{
    /// Shape-level validation only.
    ///
    /// Anything that needs the attempt's materialised paper - whether the type matches the
    /// question, whether an option id belongs to it, the per-type cardinality and text limits -
    /// is decided in the service, because those produce their own stable codes
    /// (ANSWER_TYPE_MISMATCH, QUESTION_NOT_IN_ATTEMPT) rather than a field error.
    public class UpsertAnswerRequestValidator : AbstractValidator<UpsertAnswerRequest>
    {
        public UpsertAnswerRequestValidator()
        {
            RuleFor(x => x.Value)
                .NotNull().WithMessage("Value is required.");

            RuleFor(x => x.Value!.Type)
                .NotEmpty().WithMessage("Value type is required.")
                .Must(value => QuestionTypeMap.TryFromContract(value, out _))
                .WithMessage("Value type must be one of MCQ_SINGLE, MCQ_MULTI, TRUE_FALSE, SHORT_ANSWER, ESSAY.")
                .When(x => x.Value != null);

            RuleFor(x => x.DurationMs)
                .NotNull().WithMessage("DurationMs is required.");

            // Negative is malformed rather than merely implausible. An implausibly large value
            // is clamped in the service instead of rejected, per the contract.
            RuleFor(x => x.DurationMs)
                .Must(value => value >= 0).WithMessage("DurationMs must not be negative.")
                .When(x => x.DurationMs.HasValue);

            RuleFor(x => x.ClientAnsweredAtUtc)
                .NotEmpty().WithMessage("ClientAnsweredAtUtc is required.");

            RuleFor(x => x.ClientAnsweredAtUtc)
                .Must(value => UtcTimestamp.TryParse(value, out _))
                .WithMessage("ClientAnsweredAtUtc must be an ISO-8601 UTC value, for example 2026-08-08T10:00:00Z.")
                .When(x => !string.IsNullOrWhiteSpace(x.ClientAnsweredAtUtc));
        }
    }
}
