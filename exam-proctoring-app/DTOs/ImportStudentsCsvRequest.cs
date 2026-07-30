namespace ExamProctoring.API.DTOs
{
    public class ImportStudentsCsvRequest
    {
        public required IFormFile CsvFile { get; set; }
    }
}
