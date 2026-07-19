using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Application.Features.ExamSessions.DTOs;

namespace ExamProctoring.Application.Features.ExamSessions.Services
{
    public class ExamSessionService : IExamSessionService
    {
        private readonly IExamSessionRepository _examSessionRepository;

        public ExamSessionService(IExamSessionRepository examSessionRepository)
        {
            _examSessionRepository = examSessionRepository;
        }

        public async Task<IEnumerable<ExamSessionDto>> GetAllSessionsAsync(int page, int pageSize)
        {
            var sessions = await _examSessionRepository.GetAllSessionsAsync(page, pageSize);

            return sessions.Select(s => new ExamSessionDto
            {
                Id = s.id,
                Title = s.title,
                CourseTag = s.course_tag,
                Status = s.status.ToString(),
                StartTime = s.start_time,
                DurationMinutes = s.duration_minutes,
                QuestionBankId = s.question_bank_id,
                QuestionBankName = s.QuestionBank?.title ?? "N/A",
                LockedAt = s.locked_at,
                GracePeriodMinutes = s.grace_period_minutes,
                LoginWindowMinutes = s.login_window_minutes,
                EyeGazeThresholdSec = s.eye_gaze_threshold_sec,
                ClosedAt = s.closed_at,
                CreatedAt = s.created_at,
                CreatedBy = s.created_by,
                UpdatedAt = s.updated_at,
                UpdatedBy = s.updated_by
            });
        }

        public async Task<IEnumerable<WeeklyExamSessionStatsDto>> GetWeeklyStatisticsAsync()
        {
            var data = (await _examSessionRepository.GetWeeklyStatisticsAsync()).ToList();

            var days = new[]
            {
            DayOfWeek.Saturday,
                 DayOfWeek.Sunday,
              DayOfWeek.Monday,
             DayOfWeek.Tuesday,
               DayOfWeek.Wednesday,
                DayOfWeek.Thursday,
              DayOfWeek.Friday
    };

            var result = days.Select(day =>
            {
                var item = data.FirstOrDefault(x => x.Day == day.ToString());

                return item ?? new WeeklyExamSessionStatsDto
                {
                    Day = day.ToString(),
                    TotalSessions = 0,
                    TotalStudents = 0
                };
            });

            return result;
        }
    }
}