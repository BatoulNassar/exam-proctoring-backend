using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamProctoring.Domain.Enums
{
    public enum StudentSessionStatus
    {
        NotStarted = 1,   
        InExam = 2,       
        Submitted = 3,    
        Terminated = 4,  
        Disconnected = 5
    }
}
