using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Application.Features.ExamAttempts.Services;
using ExamProctoring.Domain.Entities;
using ExamProctoring.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExamProctoring.Infrastructure.Persistence.Repositories
{
    /// Persistence for one-time auto-grading.
    ///
    /// Two things here are load-bearing:
    ///
    /// 1. <see cref="TryPersistAsync"/> writes the AutoScore rows and claims graded_at_utc in
    ///    ONE transaction. Either both land or neither does, so an attempt is never left with
    ///    half a score sheet and no way to tell.
    /// 2. <see cref="GetFrozenGradingAsync"/> never touches the Question table. Everything it
    ///    reads - the materialised paper, the saved answers, the AutoScore rows - is immutable
    ///    once the attempt is finalised, which is what makes a replay months later return the
    ///    same numbers even after the question bank has been edited.
    public class AttemptGradingRepository : IAttemptGradingRepository
    {
        private const int UniqueConstraintViolation = 2627;
        private const int UniqueIndexViolation = 2601;

        private readonly AppDbContext _context;

        public AttemptGradingRepository(AppDbContext context)
        {
            _context = context;
        }

        /// SERVER-ONLY. The only query in the codebase that reads Question.correct_answer on a
        /// path a student can trigger, and the reason IAttemptGradingRepository exists as its
        /// own seam rather than living on IAttemptRepository.
        public async Task<GradingSourceView?> GetGradingSourceAsync(int studentSessionId)
        {
            var questions = await _context.AttemptQuestions
                .AsNoTracking()
                .Where(aq => aq.student_session_id == studentSessionId)
                .OrderBy(aq => aq.ordinal)
                .Select(aq => new GradingQuestionSourceView
                {
                    QuestionId = aq.question_id,
                    PublicId = aq.public_id,
                    Ordinal = aq.ordinal,
                    Type = aq.type,
                    Marks = aq.marks,
                    CorrectAnswer = aq.Question.correct_answer,

                    Options = aq.Options
                        .OrderBy(o => o.ordinal)
                        .Select(o => new GradingOptionSourceView
                        {
                            PublicId = o.public_id,
                            SourceSlot = o.source_slot,
                            Label = o.label,
                        })
                        .ToList(),

                    StoredResponse = _context.StudentAnswers
                        .Where(sa => sa.student_session_id == studentSessionId
                                     && sa.question_id == aq.question_id)
                        .Select(sa => sa.student_response)
                        .FirstOrDefault(),
                })
                .ToListAsync();

            return questions.Count == 0 ? null : new GradingSourceView { Questions = questions };
        }

        public async Task<FrozenGradingView?> GetFrozenGradingAsync(int studentSessionId)
        {
            var gradedAtUtc = await _context.StudentSessions
                .AsNoTracking()
                .Where(ss => ss.id == studentSessionId)
                .Select(ss => ss.graded_at_utc)
                .FirstOrDefaultAsync();

            if (gradedAtUtc == null)
                return null;

            // AutoScore is keyed by the authored question id, which is also carried on the
            // materialised row, so the join needs no bank read.
            var scores = await _context.AutoScores
                .AsNoTracking()
                .Where(a => a.student_session_id == studentSessionId)
                .Select(a => new { a.question_id, a.marks_awarded, a.student_answer })
                .ToListAsync();

            var scoreByQuestion = scores
                .GroupBy(s => s.question_id)
                .ToDictionary(g => g.Key, g => g.First());

            var questions = await _context.AttemptQuestions
                .AsNoTracking()
                .Where(aq => aq.student_session_id == studentSessionId)
                .OrderBy(aq => aq.ordinal)
                .Select(aq => new
                {
                    aq.question_id,
                    aq.public_id,
                    aq.ordinal,
                    aq.type,
                    aq.marks,
                    StoredResponse = _context.StudentAnswers
                        .Where(sa => sa.student_session_id == studentSessionId
                                     && sa.question_id == aq.question_id)
                        .Select(sa => sa.student_response)
                        .FirstOrDefault(),
                })
                .ToListAsync();

            var view = new FrozenGradingView
            {
                GradedAtUtc = gradedAtUtc.Value,
                Questions = questions.Select(q =>
                {
                    scoreByQuestion.TryGetValue(q.question_id, out var score);

                    return new FrozenGradingQuestionView
                    {
                        PublicId = q.public_id,
                        Ordinal = q.ordinal,
                        Type = q.type,
                        Marks = q.marks,

                        // Null for manual questions, which never receive an AutoScore row.
                        MarksAwarded = score?.marks_awarded,

                        // For an auto question the persisted slot list is authoritative: it is
                        // what was actually scored. Falling back to the saved answer would let a
                        // later edit change a frozen result, which is the whole thing this
                        // snapshot exists to prevent.
                        WasAnswered = score != null
                            ? !string.IsNullOrEmpty(score.student_answer)
                            : HasContent(q.StoredResponse),
                    };
                }).ToList(),
            };

            return view;
        }

        public async Task<GradingPersistOutcome> TryPersistAsync(GradingPersistCommand command)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            // One conditional UPDATE evaluated by SQL Server against the current row. Two
            // concurrent finalisation paths cannot both satisfy "graded_at_utc IS NULL", so
            // exactly one grades and the other rolls its inserts back.
            var claimed = await _context.StudentSessions
                .Where(ss => ss.id == command.StudentSessionId && ss.graded_at_utc == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(ss => ss.graded_at_utc, command.NowUtc)
                    .SetProperty(ss => ss.awarded_marks, command.CurrentGrade)
                    .SetProperty(ss => ss.updated_at, command.NowUtc));

            if (claimed == 0)
            {
                await transaction.RollbackAsync();
                return GradingPersistOutcome.AlreadyGraded;
            }

            foreach (var score in command.Scores)
            {
                await _context.AutoScores.AddAsync(new AutoScore
                {
                    student_session_id = command.StudentSessionId,
                    question_id = score.QuestionId,
                    marks_awarded = score.MarksAwarded,
                    max_marks = score.MaxMarks,
                    student_answer = score.StudentAnswer,
                    correct_answer = score.CorrectAnswer,
                    created_at = command.NowUtc,
                });
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                // The unique index on (student_session_id, question_id) is the backstop: a
                // concurrent grader that somehow got past the claim cannot leave duplicate
                // scores behind. Rolling back also releases this caller's claim.
                Detach();
                await transaction.RollbackAsync();
                return GradingPersistOutcome.AlreadyGraded;
            }

            await transaction.CommitAsync();
            return GradingPersistOutcome.Persisted;
        }

        /// Answered means semantically non-empty, not merely "a row exists". A deliberately
        /// cleared answer is stored as real JSON with an empty payload, so a string check would
        /// count it as answered and inflate the manual answered count. The codec already owns
        /// that rule for the whole system; duplicating it here is how the two would drift.
        private static bool HasContent(string? storedResponse) =>
            !AnswerValueCodec.IsCleared(AnswerValueCodec.Decode(storedResponse));

        private void Detach()
        {
            foreach (var entry in _context.ChangeTracker.Entries<AutoScore>().ToList())
                entry.State = EntityState.Detached;
        }

        private static bool IsUniqueViolation(DbUpdateException exception) =>
            exception.InnerException is SqlException sql
            && (sql.Number == UniqueConstraintViolation || sql.Number == UniqueIndexViolation);
    }
}
