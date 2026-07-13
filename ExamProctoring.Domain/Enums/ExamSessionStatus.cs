using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamProctoring.Domain.Enums
{
    public enum ExamSessionStatus
    {
        DRAFT=1,
        SCHEDULED=2,
        ACTIVE=3,
        GRACE=4,
        CLOSED=5,
        ARCHIVED=6,
    }
}
