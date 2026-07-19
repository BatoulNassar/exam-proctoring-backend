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
    }
}
