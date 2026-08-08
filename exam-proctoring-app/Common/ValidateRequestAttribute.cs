using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ExamProctoring.API.Common
{
    /// Runs the registered FluentValidation validator for each bound action argument and
    /// short-circuits with the project's existing 400 / VALIDATION_FAILED envelope.
    ///
    /// This project registers validators via AddValidatorsFromAssembly but deliberately does NOT
    /// enable automatic MVC validation, so every action had to call ValidateAsync itself and
    /// rebuild the same camelCased error map. That duplication is what this filter removes; the
    /// emitted response is unchanged.
    ///
    /// Applied per-controller rather than globally on purpose: turning validation on for the
    /// dashboard controllers that never had it would silently change their behaviour.
    public sealed class ValidateRequestAttribute : ActionFilterAttribute
    {
        public override async Task OnActionExecutionAsync(
            ActionExecutingContext context, ActionExecutionDelegate next)
        {
            foreach (var argument in context.ActionArguments.Values)
            {
                if (argument == null)
                    continue;

                var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());

                if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
                    continue;

                var validationContext = new ValidationContext<object>(argument);
                var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);

                if (result.IsValid)
                    continue;

                var errors = result.Errors
                    .GroupBy(failure => ToCamelCase(failure.PropertyName))
                    .ToDictionary(group => group.Key, group => group.Select(f => f.ErrorMessage).ToArray());

                context.Result = ApiResults.ValidationFailed(errors);
                return;
            }

            await next();
        }

        private static string ToCamelCase(string propertyName) =>
            string.IsNullOrEmpty(propertyName)
                ? propertyName
                : char.ToLowerInvariant(propertyName[0]) + propertyName.Substring(1);
    }
}
