using ExamProctoring.Application.Common;
using ExamProctoring.Application.Features.ExamAttempts.DTOs;
using ExamProctoring.Application.Features.ExamAttempts.Services;
using FluentValidation;

namespace ExamProctoring.Application.Features.ExamAttempts.Validators
{
    /// Shape-level validation only, matching the pattern used by the other student endpoints.
    public class SubmitAttemptRequestValidator : AbstractValidator<SubmitAttemptRequest>
    {
        public SubmitAttemptRequestValidator()
        {
            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("Reason is required.");

            // Only the four client-facing reasons are accepted. SERVER_EXPIRY is deliberately not
            // one of them: only the server may conclude that nobody submitted.
            RuleFor(x => x.Reason)
                .Must(value => SubmitReasonMap.TryFromContract(value, out _))
                .WithMessage("Reason must be one of STUDENT_SUBMIT, CLIENT_TIMER_EXPIRED, " +
                             "PROCTOR_TERMINATED, CONNECTIVITY_RECOVERY.")
                .When(x => !string.IsNullOrWhiteSpace(x.Reason));

            RuleFor(x => x.ClientTimeUtc)
                .NotEmpty().WithMessage("ClientTimeUtc is required.");

            RuleFor(x => x.ClientTimeUtc)
                .Must(value => UtcTimestamp.TryParse(value, out _))
                .WithMessage("ClientTimeUtc must be an ISO-8601 UTC value, for example 2026-08-08T10:00:00Z.")
                .When(x => !string.IsNullOrWhiteSpace(x.ClientTimeUtc));
        }
    }
}
