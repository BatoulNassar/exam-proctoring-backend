using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Application.Features.ExamAttempts.DTOs;
using ExamProctoring.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ExamProctoring.Application.Features.ExamAttempts.Services
{
    /// Projects frozen per-question facts into the response shape, and refuses to emit a
    /// payload that contradicts itself.
    ///
    /// One builder for both paths - the first grading pass and every later replay - so the two
    /// cannot drift into producing different totals from the same data.
    public static class GradingSnapshotBuilder
    {
        public static GradingSnapshotDto Build(FrozenGradingView frozen)
        {
            var ordered = frozen.Questions.OrderBy(q => q.Ordinal).ToList();

            var auto = ordered.Where(q => AttemptAutoGrader.IsAutoGraded(q.Type)).ToList();
            var manual = ordered.Where(q => !AttemptAutoGrader.IsAutoGraded(q.Type)).ToList();
            var mcq = auto.Where(q => AttemptAutoGrader.IsMcq(q.Type)).ToList();
            var trueFalse = auto.Where(q => q.Type == QuestionType.TrueFalse).ToList();

            var autoAwarded = auto.Sum(q => q.MarksAwarded ?? 0);
            var pendingMaxMarks = manual.Sum(q => q.Marks);

            var snapshot = new GradingSnapshotDto
            {
                // COMPLETE is never sent from submit: manual marks do not exist yet, and
                // claiming completeness would tell a student their grade is final when a
                // professor still has essays to mark.
                Status = manual.Count == 0
                    ? GradingStatuses.AutoComplete
                    : GradingStatuses.PendingManual,

                AutoGradedAtUtc = DateTime.SpecifyKind(frozen.GradedAtUtc, DateTimeKind.Utc),

                Summary = new GradingSummaryDto
                {
                    CurrentGrade = autoAwarded,
                    ExamMaxMarks = ordered.Sum(q => q.Marks),
                    PossibleFinalMin = autoAwarded,
                    PossibleFinalMax = autoAwarded + pendingMaxMarks,
                },

                Auto = new GradingAutoDto
                {
                    AwardedMarks = autoAwarded,
                    MaxMarks = auto.Sum(q => q.Marks),
                    McqAwardedMarks = mcq.Sum(q => q.MarksAwarded ?? 0),
                    McqMaxMarks = mcq.Sum(q => q.Marks),
                    TrueFalseAwardedMarks = trueFalse.Sum(q => q.MarksAwarded ?? 0),
                    TrueFalseMaxMarks = trueFalse.Sum(q => q.Marks),
                    QuestionCount = auto.Count,
                    AnsweredCount = auto.Count(q => q.WasAnswered),
                    CorrectCount = auto.Count(IsCorrect),
                },

                Manual = new GradingManualDto
                {
                    // 0 rather than null when nothing is pending: null means "a human still owes
                    // you marks", and saying that when the paper has no manual questions would
                    // make a final grade look provisional.
                    AwardedMarks = manual.Count == 0 ? 0 : null,
                    PendingMaxMarks = pendingMaxMarks,
                    QuestionCount = manual.Count,
                    AnsweredCount = manual.Count(q => q.WasAnswered),
                    UnansweredCount = manual.Count(q => !q.WasAnswered),
                },

                Questions = ordered.Select(ToQuestionDto).ToList(),
            };

            Validate(snapshot);
            return snapshot;
        }

        /// A question scores full marks. Zero-mark questions cannot be "correct": awarding 0 of 0
        /// would otherwise count as a correct answer for a student who never answered.
        private static bool IsCorrect(FrozenGradingQuestionView q) =>
            q.Marks > 0 && q.MarksAwarded.HasValue && q.MarksAwarded.Value == q.Marks;

        private static GradingQuestionDto ToQuestionDto(FrozenGradingQuestionView q)
        {
            var isAuto = AttemptAutoGrader.IsAutoGraded(q.Type);

            return new GradingQuestionDto
            {
                QuestionId = q.PublicId,
                Ordinal = q.Ordinal,
                Type = QuestionTypeMap.ToContract(q.Type),
                MaxMarks = q.Marks,
                GradingMethod = isAuto ? GradingMethods.Auto : GradingMethods.Manual,
                Result = isAuto ? AutoResult(q) : GradingResults.PendingManual,

                // Integer for every auto row including zero; null for every manual row, because
                // "not yet marked" and "marked zero" are different facts.
                MarksAwarded = isAuto ? q.MarksAwarded ?? 0 : null,
            };
        }

        private static string AutoResult(FrozenGradingQuestionView q)
        {
            if (!q.WasAnswered)
                return GradingResults.Unanswered;

            return IsCorrect(q) ? GradingResults.Correct : GradingResults.Incorrect;
        }

        /// Every invariant the contract states, checked before the payload can leave the server.
        ///
        /// These are assertions about our own arithmetic, not about user input, so a failure is
        /// a bug. Throwing turns it into a 500 the client retries, which is strictly better than
        /// a receipt whose headline number disagrees with the questions listed beneath it -
        /// exactly the sort of inconsistency a student would (rightly) escalate.
        public static void Validate(GradingSnapshotDto s)
        {
            var problems = new List<string>();

            void Require(bool condition, string message)
            {
                if (!condition) problems.Add(message);
            }

            Require(s.Summary.CurrentGrade >= 0, "currentGrade is negative");
            Require(s.Summary.CurrentGrade <= s.Auto.MaxMarks, "currentGrade exceeds auto.maxMarks");
            Require(s.Auto.MaxMarks <= s.Summary.ExamMaxMarks, "auto.maxMarks exceeds examMaxMarks");

            Require(s.Summary.PossibleFinalMin >= 0, "possibleFinalMin is negative");
            Require(s.Summary.PossibleFinalMin <= s.Summary.PossibleFinalMax,
                "possibleFinalMin exceeds possibleFinalMax");
            Require(s.Summary.PossibleFinalMax <= s.Summary.ExamMaxMarks,
                "possibleFinalMax exceeds examMaxMarks");

            Require(s.Summary.PossibleFinalMin == s.Summary.CurrentGrade,
                "possibleFinalMin must equal currentGrade at submit");
            Require(s.Summary.PossibleFinalMax == s.Summary.CurrentGrade + s.Manual.PendingMaxMarks,
                "possibleFinalMax must equal currentGrade + manual.pendingMaxMarks");
            Require(s.Summary.ExamMaxMarks == s.Auto.MaxMarks + s.Manual.PendingMaxMarks,
                "examMaxMarks must equal auto.maxMarks + manual.pendingMaxMarks");
            Require(s.Summary.CurrentGrade == s.Auto.AwardedMarks,
                "currentGrade must equal auto.awardedMarks");

            Require(s.Auto.AwardedMarks == s.Auto.McqAwardedMarks + s.Auto.TrueFalseAwardedMarks,
                "auto.awardedMarks must equal mcqAwardedMarks + trueFalseAwardedMarks");
            Require(s.Auto.MaxMarks == s.Auto.McqMaxMarks + s.Auto.TrueFalseMaxMarks,
                "auto.maxMarks must equal mcqMaxMarks + trueFalseMaxMarks");
            Require(s.Auto.McqAwardedMarks >= 0 && s.Auto.McqAwardedMarks <= s.Auto.McqMaxMarks,
                "mcqAwardedMarks outside 0..mcqMaxMarks");
            Require(s.Auto.TrueFalseAwardedMarks >= 0 && s.Auto.TrueFalseAwardedMarks <= s.Auto.TrueFalseMaxMarks,
                "trueFalseAwardedMarks outside 0..trueFalseMaxMarks");

            Require(s.Auto.CorrectCount >= 0 && s.Auto.CorrectCount <= s.Auto.AnsweredCount,
                "correctCount outside 0..answeredCount");
            Require(s.Auto.AnsweredCount <= s.Auto.QuestionCount,
                "answeredCount exceeds auto.questionCount");

            Require(s.Manual.UnansweredCount == s.Manual.QuestionCount - s.Manual.AnsweredCount,
                "manual.unansweredCount must equal questionCount - answeredCount");

            Require(s.Questions.Count == s.Auto.QuestionCount + s.Manual.QuestionCount,
                "questions count must equal auto.questionCount + manual.questionCount");

            Require(s.Status is GradingStatuses.AutoComplete or GradingStatuses.PendingManual,
                "grading.status must be AUTO_COMPLETE or PENDING_MANUAL at submit");

            Require(s.Manual.QuestionCount == 0
                ? s.Manual.AwardedMarks == 0
                : s.Manual.AwardedMarks == null,
                "manual.awardedMarks must be 0 with no manual questions and null otherwise");

            if (problems.Count > 0)
                throw new GradingInvariantViolationException(problems);
        }
    }

    /// The computed snapshot contradicted the contract's own invariants.
    public sealed class GradingInvariantViolationException : Exception
    {
        public GradingInvariantViolationException(IReadOnlyList<string> problems)
            : base("Grading snapshot failed its invariants: " + string.Join("; ", problems))
        {
            Problems = problems;
        }

        public IReadOnlyList<string> Problems { get; }
    }
}
