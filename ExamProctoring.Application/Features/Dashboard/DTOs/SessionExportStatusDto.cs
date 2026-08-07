namespace ExamProctoring.Application.Features.Dashboard.DTOs
{
    /// <summary>
    /// Closed sessions split by whether a grading report was already exported.
    /// <see cref="Pending"/> is the same number shown on the "Ready to Export" card.
    /// </summary>
    public class SessionExportStatusDto
    {
        public int Exported { get; set; }
        public int Pending { get; set; }
    }
}
