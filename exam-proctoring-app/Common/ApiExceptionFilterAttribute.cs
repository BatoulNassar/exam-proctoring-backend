using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace ExamProctoring.API.Common
{
    /// Turns an unhandled action exception into the controller's own 500 envelope.
    ///
    /// Replaces the identical try/catch that wrapped every action body. It is NOT redundant with
    /// ExceptionLoggingMiddleware: that middleware emits a 500 with no stable code, whereas these
    /// endpoints emit their own message and (for the attempt endpoints) a SERVER_ERROR code.
    /// Removing the per-action catch without this filter would silently drop that code from the
    /// response, which is a contract change.
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class ApiExceptionFilterAttribute : ExceptionFilterAttribute
    {
        /// Client-facing message. Never contains exception detail.
        public string Message { get; init; } = "An unexpected error occurred.";

        /// Stable code, or null for the endpoints that predate the code convention and must keep
        /// emitting a 500 without one.
        public string? Code { get; init; }

        public override void OnException(ExceptionContext context)
        {
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger(context.ActionDescriptor.DisplayName ?? nameof(ApiExceptionFilterAttribute));

            logger.LogError(context.Exception,
                "Unhandled exception on {RequestMethod} {RequestPath}.",
                context.HttpContext.Request.Method, context.HttpContext.Request.Path.Value);

            context.Result = Code == null
                ? ApiResults.FailWithoutCode(StatusCodes.Status500InternalServerError, Message)
                : ApiResults.Fail(StatusCodes.Status500InternalServerError, Code, Message);

            context.ExceptionHandled = true;
        }
    }
}
