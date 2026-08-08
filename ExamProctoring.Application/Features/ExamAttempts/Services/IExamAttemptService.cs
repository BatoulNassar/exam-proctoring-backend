using ExamProctoring.Application.Features.ExamAttempts.DTOs;
using System;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Features.ExamAttempts.Services
{
    public interface IExamAttemptService
    {
        /// Starts a new attempt or resumes the existing one. Idempotent by nature: the first
        /// caller receives Started, every later caller receives Resumed with identical state.
        Task<StartAttemptResult> StartAsync(
            StartAttemptRequest request, int studentId, int examSessionId, string? deviceIdClaim);

        /// The full personalised question set for an attempt. Read-only.
        Task<GetAttemptQuestionsResult> GetQuestionsAsync(
            int studentId, int examSessionId, Guid attemptPublicId);

        /// Upserts the student's answer to one question. Idempotent on the supplied key:
        /// replaying the same key with the same semantic request returns the original response
        /// without writing again.
        Task<UpsertAnswerResult> UpsertAnswerAsync(
            UpsertAnswerRequest request,
            int studentId,
            int examSessionId,
            Guid attemptPublicId,
            Guid questionPublicId,
            string? rawIdempotencyKey);

        /// Finalises the attempt. Always succeeds with the frozen result once the attempt is
        /// terminal, whether this call finalised it or a previous one did - a retry after a lost
        /// response must never look like a failure.
        Task<SubmitAttemptResult> SubmitAsync(
            SubmitAttemptRequest request,
            int studentId,
            int examSessionId,
            Guid attemptPublicId,
            string? rawIdempotencyKey);
    }
}
