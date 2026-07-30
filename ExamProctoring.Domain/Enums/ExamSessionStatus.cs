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
        LOCKED=3,
        ACTIVE=4,
        GRACE=5,
        CLOSED=6,
        ARCHIVED=7,
    }
}
