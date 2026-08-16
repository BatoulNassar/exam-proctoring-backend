using ExamProctoring.Application.Features.ExamAttempts.DTOs;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Features.ExamAttempts.Services
{
    /// One-time auto-grading for a finalised attempt.
    ///
    /// Called from the shared finalisation path, so student submit, server auto-expiry and
    /// proctor termination all produce a graded attempt by the same rules. Grading deliberately
    /// does NOT happen on answer writes: a student changing an answer twenty times must not
    /// cause twenty scoring passes, and marks are awarded once, at the end.
    public interface IAttemptGradingService
    {
        /// Returns the attempt's frozen grading snapshot, grading it first if that has not
        /// happened yet.
        ///
        /// Safe to call repeatedly and concurrently. The first caller to win the conditional
        /// claim grades; everyone else replays the same frozen numbers. Callable on an attempt
        /// that was finalised but whose grading failed, which is how a retry recovers rather
        /// than leaving a student permanently without a receipt.
        Task<GradingSnapshotDto> EnsureGradedAsync(int studentSessionId);
    }
}
