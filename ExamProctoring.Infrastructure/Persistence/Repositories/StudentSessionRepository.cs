using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Application.Features.Eligibility.DTOs;
using ExamProctoring.Domain.Entities;
using ExamProctoring.Domain.Enums;
using ExamProctoring.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExamProctoring.Infrastructure.Persistence.Repositories
{
    public class StudentSessionRepository : IStudentSessionRepository
    {
        private readonly AppDbContext _context;

        public StudentSessionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<StudentSession?> GetByIdAsync(int studentSessionId)
        {
            return await _context.StudentSessions
                .Include(ss => ss.ExamSession)
                // The student is needed by the alert payload pushed over SignalR;
                // without it the live feed shows "N/A" instead of a name.
                .Include(ss => ss.Student)
                .FirstOrDefaultAsync(ss => ss.id == studentSessionId);
        }

        public Task UpdateAsync(StudentSession studentSession)
        {
            _context.StudentSessions.Update(studentSession);
            return Task.CompletedTask;
        }

        public async Task AddWarningMessageAsync(WarningMessage message)
        {
            await _context.WarningMessages.AddAsync(message);
        }

        public async Task AddMonitoringEventAsync(MonitoringEvent monitoringEvent)
        {
            await _context.MonitoringEvents.AddAsync(monitoringEvent);
        }

        public async Task AddAlertAsync(AlertEvent alert)
        {
            await _context.AlertEvents.AddAsync(alert);
        }

        public async Task<List<StudentAssignmentView>> GetVisibleAssignmentsAsync(int studentId)
        {
            // The global soft-delete filter removes deleted StudentSession and ExamSession rows,
            // including through the navigation used below. DRAFT sessions are excluded here because
            // a student is enrolled while the session is still unpublished.
            return await _context.StudentSessions
                .AsNoTracking()
                .Where(ss => ss.student_id == studentId
                             && ss.ExamSession.status != ExamSessionStatus.DRAFT)
                .Select(ss => new StudentAssignmentView
                {
                    StudentSessionId = ss.id,
                    StudentSessionStatus = ss.status,
                    SubmittedAt = ss.submitted_at,

                    ExamSessionId = ss.ExamSession.id,
                    Title = ss.ExamSession.title,
                    CourseTag = ss.ExamSession.course_tag,
                    ExamSessionStatus = ss.ExamSession.status,
                    StartTime = ss.ExamSession.start_time,
                    DurationMinutes = ss.ExamSession.duration_minutes,
                    ExtendedByMinutes = ss.ExamSession.extended_by_minutes,
                    LoginWindowMinutes = ss.ExamSession.login_window_minutes,
                })
                .ToListAsync();
        }
    }
}
