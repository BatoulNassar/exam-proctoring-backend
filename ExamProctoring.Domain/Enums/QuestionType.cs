using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamProctoring.Domain.Enums
{
    /// Persisted as a string, so members are append-only: the three original names must keep
    /// their exact spelling or existing Question rows stop materializing.
    /// MultipleChoice is the single-answer variant and maps to the API's MCQ_SINGLE.
    public enum QuestionType
    {
        MultipleChoice = 1,
        TrueFalse = 2,
        ShortAnswer = 3,

        /// Multiple correct answers may be selected. Maps to the API's MCQ_MULTI.
        MultipleChoiceMulti = 4,

        /// Free-form long answer. Maps to the API's ESSAY.
        Essay = 5
    }
}
