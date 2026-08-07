using System;
using System.Collections.Generic;

namespace ExamProctoring.Application.Common.DTOs
{
    /// <summary>
    /// Standard envelope for every paged endpoint. TotalCount is the number of rows
    /// matching the request across all pages, so the client can render a pager
    /// without walking the whole collection.
    /// </summary>
    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
