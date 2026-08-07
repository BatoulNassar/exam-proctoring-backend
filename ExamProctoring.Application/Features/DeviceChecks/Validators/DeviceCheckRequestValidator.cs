using ExamProctoring.Application.Features.DeviceChecks.DTOs;
using FluentValidation;
using System;
using System.Globalization;
using System.Linq;

namespace ExamProctoring.Application.Features.DeviceChecks.Validators
{
    public class DeviceCheckRequestValidator : AbstractValidator<DeviceCheckRequest>
    {
        private const int MaxRequirements = 20;
        private const int MaxDetailLength = 200;

        public DeviceCheckRequestValidator()
        {
            RuleFor(x => x.SessionId)
                .NotNull().WithMessage("Session id is required.");

            RuleFor(x => x.SessionId)
                .Must(value => value > 0).WithMessage("Session id must be greater than zero.")
                .When(x => x.SessionId.HasValue);

            RuleFor(x => x.DeviceId)
                .NotEmpty().WithMessage("Device ID is required.");

            RuleFor(x => x.DeviceId)
                .Must(value => Guid.TryParse(value, out _))
                .WithMessage("Device ID must be a valid UUID.")
                .When(x => !string.IsNullOrWhiteSpace(x.DeviceId));

            RuleFor(x => x.CheckedAtUtc)
                .NotEmpty().WithMessage("CheckedAtUtc is required.");

            RuleFor(x => x.CheckedAtUtc)
                .Must(value => TryParseUtc(value, out _))
                .WithMessage("CheckedAtUtc must be an ISO-8601 UTC value, for example 2026-08-06T18:30:00Z.")
                .When(x => !string.IsNullOrWhiteSpace(x.CheckedAtUtc));

            RuleFor(x => x.CanProceed)
                .NotNull().WithMessage("CanProceed is required.");

            RuleFor(x => x.Requirements)
                .NotNull().WithMessage("Requirements are required.");

            RuleFor(x => x.Requirements)
                .Must(list => list!.Count >= 1 && list.Count <= MaxRequirements)
                .WithMessage($"Requirements must contain between 1 and {MaxRequirements} entries.")
                .When(x => x.Requirements != null);

            RuleFor(x => x.Requirements)
                .Must(HasNoDuplicateIds)
                .WithMessage("Duplicate requirement identifiers are not allowed.")
                .When(x => x.Requirements != null && x.Requirements.Count > 0);

            RuleForEach(x => x.Requirements).ChildRules(requirement =>
            {
                requirement.RuleFor(r => r.Id)
                    .NotEmpty().WithMessage("Requirement id is required.");

                requirement.RuleFor(r => r.Id)
                    .MaximumLength(50).WithMessage("Requirement id must not exceed 50 characters.")
                    .Must(value => DeviceCheckRequirementIds.Supported.Contains(value!))
                    .WithMessage("Requirement id is not supported.")
                    .When(r => !string.IsNullOrWhiteSpace(r.Id));

                requirement.RuleFor(r => r.Status)
                    .NotEmpty().WithMessage("Requirement status is required.");

                requirement.RuleFor(r => r.Status)
                    .Must(value => DeviceCheckStatusValues.Supported.Contains(value!))
                    .WithMessage("Requirement status must be PASSED, WARNING or FAILED.")
                    .When(r => !string.IsNullOrWhiteSpace(r.Status));

                requirement.RuleFor(r => r.Detail)
                    .MaximumLength(MaxDetailLength)
                    .WithMessage($"Requirement detail must not exceed {MaxDetailLength} characters.")
                    .Must(ContainsNoControlCharacters)
                    .WithMessage("Requirement detail must not contain control characters.")
                    .When(r => r.Detail != null);
            });
        }

        /// Accepts only values carrying an explicit UTC designator - a trailing Z or a zero
        /// offset. A value without timezone information is rejected rather than assumed
        /// local, so the result never depends on the server's own time zone.
        public static bool TryParseUtc(string? value, out DateTime utcValue)
        {
            utcValue = default;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            var text = value.Trim();

            var hasUtcDesignator = text.EndsWith("Z", StringComparison.OrdinalIgnoreCase)
                                   || text.EndsWith("+00:00", StringComparison.Ordinal)
                                   || text.EndsWith("-00:00", StringComparison.Ordinal);

            if (!hasUtcDesignator)
                return false;

            if (!DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out var parsed))
                return false;

            if (parsed.Offset != TimeSpan.Zero)
                return false;

            utcValue = parsed.UtcDateTime;
            return true;
        }

        private static bool HasNoDuplicateIds(System.Collections.Generic.List<DeviceCheckRequirementDto>? requirements)
        {
            var ids = requirements!
                .Select(r => r.Id)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToList();

            return ids.Count == ids.Distinct(StringComparer.Ordinal).Count();
        }

        /// Rejects C0, DEL and C1 control characters, which would otherwise reach
        /// structured logs and reporting output.
        private static bool ContainsNoControlCharacters(string? detail)
        {
            if (string.IsNullOrEmpty(detail))
                return true;

            return !detail.Any(char.IsControl);
        }
    }
}
