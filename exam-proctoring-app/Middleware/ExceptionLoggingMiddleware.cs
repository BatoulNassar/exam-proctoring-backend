using ExamProctoring.API.Common;
using System.Text.Json;

namespace ExamProctoring.API.Middleware
{
    /// Logs every unhandled request exception with full diagnostic detail and returns a generic
    /// response. Exception details are never sent to the client.
    public class ExceptionLoggingMiddleware
    {
        private static readonly JsonSerializerOptions SerializerOptions =
            new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionLoggingMiddleware> _logger;

        public ExceptionLoggingMiddleware(RequestDelegate next, ILogger<ExceptionLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                // Structured detail for the server log only: type, message, request, inner chain.
                // The stack trace travels with the exception object passed as the first argument.
                _logger.LogError(ex,
                    "Unhandled exception {ExceptionType} on {RequestMethod} {RequestPath}: {ExceptionMessage} | Inner: {InnerChain}",
                    ex.GetType().FullName,
                    context.Request.Method,
                    context.Request.Path.Value,
                    ex.Message,
                    DescribeInnerExceptions(ex));

                if (context.Response.HasStarted)
                    throw;

                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";

                var body = ApiResponse<object>.Fail("An unexpected error occurred.", StatusCodes.Status500InternalServerError);
                await context.Response.WriteAsync(JsonSerializer.Serialize(body, SerializerOptions));
            }
        }

        private static string DescribeInnerExceptions(Exception exception)
        {
            var descriptions = new List<string>();

            for (var inner = exception.InnerException; inner != null; inner = inner.InnerException)
                descriptions.Add($"{inner.GetType().FullName}: {inner.Message}");

            return descriptions.Count == 0 ? "(none)" : string.Join(" -> ", descriptions);
        }
    }
}
