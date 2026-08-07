using System;

namespace ExamProctoring.Application.Features.Dashboard.DTOs
{
    public class SessionCountByDayDto
    {
        /// <summary>Date of the bucket, so the client can sort reliably.</summary>
        public DateTime Date { get; set; }

        /// <summary>Day name for the chart axis label, e.g. "Monday".</summary>
        public string DayName { get; set; }

        public int SessionCount { get; set; }
        public int StudentCount { get; set; }
    }
}
