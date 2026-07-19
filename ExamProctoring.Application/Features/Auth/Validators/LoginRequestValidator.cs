using ExamProctoring.Application.Features.Auth.DTOs;
using FluentValidation;

namespace ExamProctoring.Application.Features.Auth.Validators
{
    public class LoginRequestValidator : AbstractValidator<LoginRequestDto>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email address format.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.");
        }
    }
}