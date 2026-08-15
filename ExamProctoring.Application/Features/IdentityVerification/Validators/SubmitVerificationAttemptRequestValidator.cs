using ExamProctoring.Application.Common;
using ExamProctoring.Application.Features.IdentityVerification.DTOs;
using FluentValidation;
using System;

namespace ExamProctoring.Application.Features.IdentityVerification.Validators
{
    /// Shape-level validation only.
    ///
    /// Deliberately does NOT validate the embedding's length, finiteness or norm: those produce
    /// the contract's own IDV_EMBEDDING_INVALID code rather than a field-error envelope, so
    /// they are decided in the service. Likewise the model and version are checked there,
    /// because a mismatch is a 409 with its own code and must not consume an attempt.
    ///
    /// What is checked here is only what would otherwise crash or bind meaninglessly.
    public class SubmitVerificationAttemptRequestValidator
        : AbstractValidator<SubmitVerificationAttemptRequest>
    {
        private const int MaxIdentifierLength = 100;

        public SubmitVerificationAttemptRequestValidator()
        {
            RuleFor(x => x.Embedding)
                .NotNull().WithMessage("Embedding is required.");

            RuleFor(x => x.EmbeddingModel)
                .NotEmpty().WithMessage("EmbeddingModel is required.")
                .MaximumLength(MaxIdentifierLength)
                .WithMessage($"EmbeddingModel must not exceed {MaxIdentifierLength} characters.");

            RuleFor(x => x.EmbeddingVersion)
                .NotEmpty().WithMessage("EmbeddingVersion is required.")
                .MaximumLength(MaxIdentifierLength)
                .WithMessage($"EmbeddingVersion must not exceed {MaxIdentifierLength} characters.");

            // The idempotency key is the one thing that must be well-formed before anything
            // else happens: without it a retry cannot be recognised and a student loses an
            // attempt to a dropped connection. The contract's example is not a bare UUID
            // ("…-1"), so any stable non-empty token is accepted rather than requiring a Guid.
            RuleFor(x => x.ClientAttemptId)
                .NotEmpty().WithMessage("ClientAttemptId is required.")
                .MaximumLength(MaxIdentifierLength)
                .WithMessage($"ClientAttemptId must not exceed {MaxIdentifierLength} characters.");

            RuleFor(x => x.Liveness)
                .NotNull().WithMessage("Liveness evidence is required.");

            When(x => x.Liveness != null, () =>
            {
                RuleFor(x => x.Liveness!.BlinkCount)
                    .NotNull().WithMessage("Liveness blinkCount is required.");

                RuleFor(x => x.Liveness!.FramesAnalysed)
                    .NotNull().WithMessage("Liveness framesAnalysed is required.");

                RuleFor(x => x.Liveness!.DurationMs)
                    .NotNull().WithMessage("Liveness durationMs is required.");

                RuleFor(x => x.Liveness!.MinEyeOpenness)
                    .NotNull().WithMessage("Liveness minEyeOpenness is required.");

                RuleFor(x => x.Liveness!.MaxEyeOpenness)
                    .NotNull().WithMessage("Liveness maxEyeOpenness is required.");
            });

            RuleFor(x => x.CapturedAtUtc)
                .NotEmpty().WithMessage("CapturedAtUtc is required.");

            RuleFor(x => x.CapturedAtUtc)
                .Must(value => UtcTimestamp.TryParse(value, out _))
                .WithMessage("CapturedAtUtc must be an ISO-8601 UTC value, for example 2026-08-14T09:14:22Z.")
                .When(x => !string.IsNullOrWhiteSpace(x.CapturedAtUtc));
        }
    }
}
