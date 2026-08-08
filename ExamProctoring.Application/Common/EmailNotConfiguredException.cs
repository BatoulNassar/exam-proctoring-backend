using System;

namespace ExamProctoring.Application.Common
{
    /// <summary>
    /// The mail settings are missing or incomplete. A server fault, not a caller
    /// mistake, so it must not be reported back as a 400 with its message: the
    /// endpoints that send mail are unauthenticated, and the text names internal
    /// configuration keys. Callers let it reach the generic handler, which logs the
    /// detail and answers 500.
    /// </summary>
    public class EmailNotConfiguredException : Exception
    {
        public EmailNotConfiguredException(string message) : base(message)
        {
        }
    }
}
