using Microsoft.AspNetCore.Mvc;

namespace ExamProctoring.API.Common
{
    /// Builds the project's ApiResponse envelope as an IActionResult without needing
    /// ControllerBase, so result mapping can live outside the controller.
    ///
    /// This changes no response shape - it is the same ApiResponse, the same status codes and the
    /// same stable codes as before, just constructed in one place instead of inline in every action.
    public static class ApiResults
    {
        public static IActionResult Ok<T>(T data, string message) =>
            new ObjectResult(ApiResponse<T>.Ok(data, message)) { StatusCode = StatusCodes.Status200OK };

        public static IActionResult Created<T>(T data, string message) =>
            new ObjectResult(ApiResponse<T>.Ok(data, message, StatusCodes.Status201Created))
            { StatusCode = StatusCodes.Status201Created };

        public static IActionResult Fail(int status, string code, string message, object? details = null) =>
            new ObjectResult(ApiResponse<object>.Fail(message, status, code, details)) { StatusCode = status };

        /// Failure with no stable code, matching the handful of endpoints that predate the code
        /// convention. Kept so their responses stay byte-identical.
        public static IActionResult FailWithoutCode(int status, string message) =>
            new ObjectResult(ApiResponse<object>.Fail(message, status)) { StatusCode = status };

        // ----- shared failures used by more than one student endpoint -----

        public static IActionResult AccountInactive() => Fail(
            StatusCodes.Status403Forbidden, AuthErrorCodes.AccountInactive, "Student account is inactive.");

        /// The field-error envelope used by every validated student endpoint.
        public static IActionResult ValidationFailed(IDictionary<string, string[]> errors) => Fail(
            StatusCodes.Status400BadRequest, AuthErrorCodes.ValidationFailed,
            "Validation failed.", new { errors });

        public static IActionResult ValidationFailed(string field, string message) =>
            ValidationFailed(new Dictionary<string, string[]> { [field] = new[] { message } });
    }
}
