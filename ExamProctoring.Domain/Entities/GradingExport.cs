using ExamProctoring.Domain.Common;
using ExamProctoring.Domain.Entities;
using ExamProctoring.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamProctoring.Domain.Entities
{
    public class GradingExport : BaseEntity
    {
        public int exam_session_id { get; set; }
        public ExportFormat format { get; set; }
        public string file_path { get; set; }
        public DateTime generated_at { get; set; }

        public ExamSession ExamSession { get; set; }    

    } 
}
