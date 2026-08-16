using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Application.Features.ExamAttempts.DTOs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Features.ExamAttempts.Services
{
    /// Grades a finalised attempt exactly once and replays the frozen result thereafter.
    ///
    /// The flow is deliberately read-then-claim-then-read:
    ///
    ///   1. If the attempt is already graded, project its frozen state and return.
    ///   2. Otherwise score the paper and try to claim the one-time grading marker.
    ///   3. If the claim was lost to a concurrent finaliser, re-read and project the winner's
    ///      state - never our own computed numbers, so both callers return byte-identical
    ///      results even in a race.
    ///
    /// Step 3 matters more than it looks. Two paths can finalise the same attempt in the same
    /// instant (a student pressing Submit as the expiry janitor sweeps them up); if each
    /// returned its own arithmetic the two receipts could disagree, and only one of them is in
    /// the database.
    public class AttemptGradingService : IAttemptGradingService
    {
        private readonly IAttemptGradingRepository _gradingRepository;
        private readonly ILogger<AttemptGradingService> _logger;

        public AttemptGradingService(
            IAttemptGradingRepository gradingRepository,
            ILogger<AttemptGradingService> logger)
        {
            _gradingRepository = gradingRepository;
            _logger = logger;
        }

        public async Task<GradingSnapshotDto> EnsureGradedAsync(int studentSessionId)
        {
            var frozen = await _gradingRepository.GetFrozenGradingAsync(studentSessionId);
            if (frozen != null)
                return GradingSnapshotBuilder.Build(frozen);

            var source = await _gradingRepository.GetGradingSourceAsync(studentSessionId);

            if (source == null)
                throw new InvalidOperationException(
                    $"Attempt {studentSessionId} has no materialised paper to grade.");

            var nowUtc = DateTime.UtcNow;
            var scores = new List<AutoScoreRecord>(source.Questions.Count);
            var currentGrade = 0;

            foreach (var question in source.Questions.OrderBy(q => q.Ordinal))
            {
                // Manual questions get no AutoScore row at all. Their absence is what makes
                // "pending" reconstructible later without a second marker column.
                if (!AttemptAutoGrader.IsAutoGraded(question.Type))
                    continue;

                var result = AttemptAutoGrader.Grade(question);
                currentGrade += result.MarksAwarded;

                scores.Add(new AutoScoreRecord
                {
                    QuestionId = question.QuestionId,
                    MarksAwarded = result.MarksAwarded,
                    MaxMarks = question.Marks,
                    StudentAnswer = result.SelectedSlots,
                    CorrectAnswer = result.CorrectSlots,
                });
            }

            var outcome = await _gradingRepository.TryPersistAsync(new GradingPersistCommand
            {
                StudentSessionId = studentSessionId,
                NowUtc = nowUtc,
                CurrentGrade = currentGrade,
                Scores = scores,
            });

            // Re-read in both cases. On a win this proves what was actually committed rather
            // than trusting in-memory arithmetic; on a loss it is the only correct answer.
            var persisted = await _gradingRepository.GetFrozenGradingAsync(studentSessionId);

            if (persisted == null)
                throw new InvalidOperationException(
                    $"Attempt {studentSessionId} reported grading outcome {outcome} but no frozen snapshot could be read back.");

            if (outcome == GradingPersistOutcome.Persisted)
                _logger.LogInformation(
                    "Attempt {StudentSessionId} auto-graded: {AutoCount} auto question(s), currentGrade={CurrentGrade}.",
                    studentSessionId, scores.Count, currentGrade);
            else
                _logger.LogInformation(
                    "Attempt {StudentSessionId} was already graded by a concurrent finalisation; replaying the frozen snapshot.",
                    studentSessionId);

            return GradingSnapshotBuilder.Build(persisted);
        }
    }
}
