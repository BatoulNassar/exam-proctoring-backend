using ExamProctoring.Application.Common;
using ExamProctoring.Application.Features.StudentAuth.DTOs;
using FluentValidation;
using System;

namespace ExamProctoring.Application.Features.StudentAuth.Validators
{
    public class StudentLoginRequestValidator : AbstractValidator<StudentLoginRequest>
    {
        public StudentLoginRequestValidator()
        {
            // NotEmpty() treats null, "" and whitespace-only strings as failures.
            RuleFor(x => x.Identifier)
                .NotEmpty().WithMessage("Identifier is required and cannot be empty or whitespace.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required and cannot be empty or whitespace.");

            RuleFor(x => x.DeviceId)
                .NotEmpty().WithMessage("Device ID is required.");

            // Real UUID parsing, not a pattern match.
            RuleFor(x => x.DeviceId)
                .Must(value => Guid.TryParse(value, out _))
                .WithMessage("Device ID must be a valid UUID.")
                .When(x => !string.IsNullOrWhiteSpace(x.DeviceId));

            RuleFor(x => x.AppVersion)
                .NotEmpty().WithMessage("Application version is required.");

            RuleFor(x => x.AppVersion)
                .Must(value => AppVersion.TryParse(value, out _))
                .WithMessage("Application version must be in the format major.minor.patch or major.minor.patch+buildNumber.")
                .When(x => !string.IsNullOrWhiteSpace(x.AppVersion));
        }
    }
}
