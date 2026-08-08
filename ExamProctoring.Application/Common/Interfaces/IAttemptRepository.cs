using ExamProctoring.Application.Features.ExamAttempts.DTOs;
using ExamProctoring.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Common.Interfaces
{
    /// Persistence for the student's exam attempt (a StudentSession row) and its materialised
    /// paper. Every read is a flat projection: the authored Question row - and therefore
    /// correct_answer - is never loaded on a student-triggered path.
    public interface IAttemptRepository
    {
        /// The student's own attempt for one exam session, using the same visibility rules as
        /// eligibility: soft-deleted rows excluded and DRAFT sessions hidden. Null when the
        /// session does not exist, is not assigned to this student, is deleted or is DRAFT.
        Task<AttemptView?> GetByExamSessionAsync(int studentId, int examSessionId);

        /// Resolves an attempt by its public UUID scoped to both the owning student and the
        /// exam session, in a single query. Returns null for a missing attempt and for another
        /// student's attempt alike, so the two are indistinguishable to the caller.
        Task<AttemptView?> GetByPublicIdAsync(int studentId, int examSessionId, Guid attemptPublicId);

        /// Atomically claims the first start with a single conditional UPDATE
        /// (WHERE started_at IS NULL). Returns true only for the caller that won the race, so
        /// two concurrent Start requests can never both stamp a start time, reset the timer,
        /// or bind different devices.
        Task<bool> TryClaimFirstStartAsync(
            int studentSessionId,
            Guid publicId,
            DateTime startedAtUtc,
            DateTime endsAtUtc,
            string deviceId,
            DateTime nowUtc);

        /// Writes the materialised paper and stamps question_count in one transaction.
        /// Safe to retry: the unique index on (student_session_id, question_id) makes a
        /// duplicate insert fail rather than produce a second paper, and the method reports
        /// that case instead of throwing.
        Task<MaterialisationOutcome> MaterialiseQuestionSetAsync(
            int studentSessionId,
            IReadOnlyList<AttemptQuestion> questions,
            DateTime nowUtc);

        /// Number of already-materialised questions for an attempt. Used to detect an attempt
        /// that was claimed but whose paper never landed.
        Task<int> CountMaterialisedQuestionsAsync(int studentSessionId);

        /// Authored questions for a bank, projected without any answer-key column.
        Task<List<QuestionSourceView>> GetBankQuestionsAsync(int questionBankId);

        /// The student's materialised paper in presentation order, with options.
        Task<List<AttemptQuestionView>> GetMaterialisedPaperAsync(int studentSessionId);

        /// One question of this attempt's materialised paper, resolved by its student-facing
        /// public id. Scoped to the attempt in the query, so a question belonging to another
        /// student's attempt or to a different exam simply returns null.
        Task<AttemptQuestionTargetView?> GetAttemptQuestionAsync(int studentSessionId, Guid questionPublicId);
    }

    public enum MaterialisationOutcome
    {
        /// This caller wrote the paper.
        Created,

        /// Another caller had already written it; the existing paper stands.
        AlreadyMaterialised,
    }
}
