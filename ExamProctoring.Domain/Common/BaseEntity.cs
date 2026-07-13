namespace ExamProctoring.Domain.Common
{
    public abstract class BaseEntity
    {
        public int id { get; set; }
        public DateTime created_at { get; set; }
        public int? created_by { get; set; }
        public DateTime? updated_at { get; set; }
        public int? updated_by { get; set; }
        public bool is_deleted { get; set; } = false;
        public DateTime? deleted_at { get; set; }
        public int? deleted_by { get; set; }
    }
}