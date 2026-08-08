using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Application.Features.ExamAttempts.DTOs;
using ExamProctoring.Domain.Entities;
using ExamProctoring.Domain.Enums;
using ExamProctoring.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExamProctoring.Infrastructure.Persistence.Repositories
{
    public class AttemptRepository : IAttemptRepository
    {
        /// SQL Server unique-constraint / unique-index violations.
        private const int UniqueConstraintViolation = 2627;
        private const int UniqueIndexViolation = 2601;

        private readonly AppDbContext _context;

        public AttemptRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AttemptView?> GetByExamSessionAsync(int studentId, int examSessionId) =>
            await VisibleAttempts(studentId)
                .FirstOrDefaultAsync(a => a.ExamSessionId == examSessionId);

        public async Task<AttemptView?> GetByPublicIdAsync(int studentId, int examSessionId, Guid attemptPublicId) =>
            // Ownership is enforced inside the query, so a foreign attempt simply returns null
            // rather than being fetched and then compared.
            await VisibleAttempts(studentId)
                .FirstOrDefaultAsync(a => a.ExamSessionId == examSessionId && a.PublicId == attemptPublicId);

        /// Single definition of a "visible attempt", matching the eligibility rules: the global
        /// soft-delete filter removes deleted StudentSession and ExamSession rows, and DRAFT
        /// sessions are excluded because a student is enrolled while still unpublished.
        ///
        /// A projection rather than an entity load: Question and its correct_answer are never
        /// part of this graph.
        private IQueryable<AttemptView> VisibleAttempts(int studentId) =>
            _context.StudentSessions
                .AsNoTracking()
                .Where(ss => ss.student_id == studentId
                             && ss.ExamSession.status != ExamSessionStatus.DRAFT)
                .Select(ss => new AttemptView
                {
                    StudentSessionId = ss.id,
                    PublicId = ss.public_id,
                    Status = ss.status,
                    StartedAt = ss.started_at,
                    EndsAt = ss.ends_at,
                    DeviceId = ss.device_id,
                    QuestionCount = ss.question_count,
                    SubmittedAt = ss.submitted_at,

                    FinalisedAt = ss.finalised_at,
                    FinalisationReason = ss.finalisation_reason,
                    AnsweredCount = ss.answered_count,
                    ReceiptCode = ss.receipt_code,

                    ExamSessionId = ss.ExamSession.id,
                    ExamSessionStatus = ss.ExamSession.status,
                    CourseTag = ss.ExamSession.course_tag,
                    StartTime = ss.ExamSession.start_time,
                    DurationMinutes = ss.ExamSession.duration_minutes,
                    ExtendedByMinutes = ss.ExamSession.extended_by_minutes,
                    LoginWindowMinutes = ss.ExamSession.login_window_minutes,
                    GracePeriodEndedAt = ss.ExamSession.grace_period_ended_at,
                    EyeGazeThresholdSec = ss.ExamSession.eye_gaze_threshold_sec,

                    QuestionBankId = ss.ExamSession.question_bank_id,
                    Randomization = ss.ExamSession.QuestionBank.randomization,
                    OptionShuffle = ss.ExamSession.QuestionBank.option_shuffle,
                });

        /// One conditional UPDATE evaluated by SQL Server against the current row. Two
        /// concurrent callers cannot both satisfy "started_at IS NULL", so exactly one wins and
        /// the loser is told to resume - no read-then-write race, no double timer, no
        /// conflicting device binding.
        public async Task<bool> TryClaimFirstStartAsync(
            int studentSessionId,
            Guid publicId,
            DateTime startedAtUtc,
            DateTime endsAtUtc,
            string deviceId,
            DateTime nowUtc)
        {
            var rowsAffected = await _context.StudentSessions
                .Where(ss => ss.id == studentSessionId && ss.started_at == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(ss => ss.started_at, startedAtUtc)
                    .SetProperty(ss => ss.ends_at, endsAtUtc)
                    .SetProperty(ss => ss.device_id, deviceId)
                    .SetProperty(ss => ss.status, StudentSessionStatus.InExam)
                    .SetProperty(ss => ss.updated_at, nowUtc));

            return rowsAffected == 1;
        }

        /// The paper and its question_count land in one transaction, so an attempt can never be
        /// left advertising a count it does not have.
        public async Task<MaterialisationOutcome> MaterialiseQuestionSetAsync(
            int studentSessionId,
            IReadOnlyList<AttemptQuestion> questions,
            DateTime nowUtc)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                await _context.AttemptQuestions.AddRangeAsync(questions);
                await _context.SaveChangesAsync();

                await _context.StudentSessions
                    .Where(ss => ss.id == studentSessionId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(ss => ss.question_count, questions.Count)
                        .SetProperty(ss => ss.updated_at, nowUtc));

                await transaction.CommitAsync();

                return MaterialisationOutcome.Created;
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                // A concurrent request materialised the same paper first. Its rows stand.
                await transaction.RollbackAsync();

                // The failed inserts are still tracked; detach them so this DbContext can be
                // reused for the follow-up read without trying to write them again.
                //
                // Options are snapshotted with ToList() first: detaching an option makes the
                // change tracker fix up navigations and remove it from question.Options, which
                // would invalidate an in-flight enumeration of that same collection.
                foreach (var question in questions)
                {
                    foreach (var option in question.Options.ToList())
                        _context.Entry(option).State = EntityState.Detached;

                    _context.Entry(question).State = EntityState.Detached;
                }

                return MaterialisationOutcome.AlreadyMaterialised;
            }
        }

        private static bool IsUniqueViolation(DbUpdateException exception) =>
            exception.InnerException is SqlException sql
            && (sql.Number == UniqueConstraintViolation || sql.Number == UniqueIndexViolation);

        public async Task<int> CountMaterialisedQuestionsAsync(int studentSessionId) =>
            await _context.AttemptQuestions
                .AsNoTracking()
                .CountAsync(aq => aq.student_session_id == studentSessionId);

        /// SECURITY: correct_answer is deliberately absent from this projection, so the answer
        /// key is never read from the database on a student-triggered path.
        public async Task<List<QuestionSourceView>> GetBankQuestionsAsync(int questionBankId) =>
            await _context.Questions
                .AsNoTracking()
                .Where(q => q.question_bank_id == questionBankId)
                .OrderBy(q => q.id)
                .Select(q => new QuestionSourceView
                {
                    QuestionId = q.id,
                    Type = q.type,
                    QuestionText = q.question_text,
                    Marks = q.marks,
                    OptionA = q.option_a,
                    OptionB = q.option_b,
                    OptionC = q.option_c,
                    OptionD = q.option_d,
                    OptionE = q.option_e,
                })
                .ToListAsync();

        /// Scoped to the attempt inside the query, so a question from another student's paper,
        /// another exam, or the authored Question table simply returns null.
        /// SECURITY: source_slot is deliberately absent from this projection.
        public async Task<AttemptQuestionTargetView?> GetAttemptQuestionAsync(
            int studentSessionId, Guid questionPublicId) =>
            await _context.AttemptQuestions
                .AsNoTracking()
                .Where(aq => aq.student_session_id == studentSessionId && aq.public_id == questionPublicId)
                .Select(aq => new AttemptQuestionTargetView
                {
                    AttemptQuestionId = aq.id,
                    QuestionId = aq.question_id,
                    PublicId = aq.public_id,
                    Type = aq.type,
                    OptionPublicIds = aq.Options.Select(o => o.public_id).ToList(),
                })
                .FirstOrDefaultAsync();

        /// SECURITY: source_slot is deliberately absent from this projection.
        public async Task<List<AttemptQuestionView>> GetMaterialisedPaperAsync(int studentSessionId) =>
            await _context.AttemptQuestions
                .AsNoTracking()
                .Where(aq => aq.student_session_id == studentSessionId)
                .OrderBy(aq => aq.ordinal)
                .Select(aq => new AttemptQuestionView
                {
                    PublicId = aq.public_id,
                    Ordinal = aq.ordinal,
                    Type = aq.type,
                    Stem = aq.stem,
                    Marks = aq.marks,
                    Options = aq.Options
                        .OrderBy(o => o.ordinal)
                        .Select(o => new AttemptQuestionOptionView
                        {
                            PublicId = o.public_id,
                            Ordinal = o.ordinal,
                            Label = o.label,
                        })
                        .ToList(),
                })
                .ToListAsync();
    }
}
