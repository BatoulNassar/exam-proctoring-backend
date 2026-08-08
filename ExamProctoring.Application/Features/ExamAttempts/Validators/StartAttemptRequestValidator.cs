using ExamProctoring.Application.Common;
using ExamProctoring.Application.Features.ExamAttempts.DTOs;
using FluentValidation;
using System;

namespace ExamProctoring.Application.Features.ExamAttempts.Validators
{
    /// Shape-level validation only. Anything needing the attempt or exam session state
    /// (lifecycle, identity, device binding) is decided in the service, because those produce
    /// distinct stable codes rather than a field error.
    public class StartAttemptRequestValidator : AbstractValidator<StartAttemptRequest>
    {
        private const int MaxAppVersionLength = 30;

        public StartAttemptRequestValidator()
        {
            RuleFor(x => x.DeviceId)
                .NotEmpty().WithMessage("Device ID is required.");

            RuleFor(x => x.DeviceId)
                .Must(value => Guid.TryParse(value, out _))
                .WithMessage("Device ID must be a valid UUID.")
                .When(x => !string.IsNullOrWhiteSpace(x.DeviceId));

            RuleFor(x => x.AppVersion)
                .NotEmpty().WithMessage("App version is required.");

            RuleFor(x => x.AppVersion)
                .MaximumLength(MaxAppVersionLength)
                .WithMessage($"App version must not exceed {MaxAppVersionLength} characters.")
                .Must(value => AppVersion.TryParse(value, out _))
                .WithMessage("App version must look like 1.0.0 or 1.0.0+1.")
                .When(x => !string.IsNullOrWhiteSpace(x.AppVersion));

            RuleFor(x => x.ClientTimeUtc)
                .NotEmpty().WithMessage("ClientTimeUtc is required.");

            RuleFor(x => x.ClientTimeUtc)
                .Must(value => UtcTimestamp.TryParse(value, out _))
                .WithMessage("ClientTimeUtc must be an ISO-8601 UTC value, for example 2026-08-08T18:30:00Z.")
                .When(x => !string.IsNullOrWhiteSpace(x.ClientTimeUtc));
        }
    }
}
