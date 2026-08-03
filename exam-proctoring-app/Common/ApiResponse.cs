using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ExamProctoring.API.Common
{
    public class ApiResponse<T>
    {
        [JsonPropertyOrder(3)]
        public T? Data { get; set; }
        [JsonPropertyOrder(2)]
        public string Message { get; set; } = string.Empty;
        [JsonPropertyOrder(1)]
        public int StatusCode { get; set; }

        /// Stable machine-readable error code. Omitted from JSON when null, so existing
        /// success and failure responses are serialized exactly as before.
        [JsonPropertyOrder(4)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Code { get; set; }

        /// Optional machine-readable payload accompanying an error code. Omitted when null.
        [JsonPropertyOrder(5)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Details { get; set; }

        public static ApiResponse<T> Ok(T data, string message = "Success", int statusCode = 200)
        {
            return new ApiResponse<T>
            {
                StatusCode = statusCode,
                Message = message,
                Data = data,
            };
        }

        public static ApiResponse<T> Fail(string message, int statusCode, List<string>? errors = null)
        {
            return new ApiResponse<T>
            {
                StatusCode = statusCode,
                Message = message,
                Data = default,

            };
        }

        /// Failure carrying a stable machine-readable code and optional details.
        public static ApiResponse<T> Fail(string message, int statusCode, string code, object? details = null)
        {
            return new ApiResponse<T>
            {
                StatusCode = statusCode,
                Message = message,
                Data = default,
                Code = code,
                Details = details,
            };
        }
    }
}
