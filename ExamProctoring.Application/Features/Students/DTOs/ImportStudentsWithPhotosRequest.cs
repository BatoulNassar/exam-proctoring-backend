namespace ExamProctoring.Application.Features.Students.DTOs
{
    public class ImportStudentsWithPhotosRequest
    {
        public Stream CsvStream { get; set; }
        public Stream PhotosZipStream { get; set; }
    }
}
