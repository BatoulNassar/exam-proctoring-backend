using ExamProctoring.Application.Features.ExamSessions.DTOs;
using ExamProctoring.Domain.Entities;
using ExamProctoring.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Common.Interfaces
{
    public interface IExamSessionRepository
    {
        /// <param name="adminId">
        /// Owner scope: null returns every session (SuperAdmin), a value returns only
        /// sessions that admin created. Derived from the token, never the request.
        /// </param>
        Task<IEnumerable<ExamSession>> GetAllSessionsAsync(int page, int pageSize, int? adminId = null);
        Task<int> CountAllSessionsAsync(int? adminId = null);
        Task<ExamSession?> GetByIdWithDetailsAsync(int id);
        Task<ExamSession?> GetByIdAsync(int id);
        Task<ExamSession?> GetByIdWithQuestionBankAsync(int id);
        Task AddAsync(ExamSession session);
        Task AddStudentSessionsAsync(IEnumerable<StudentSession> studentSessions);
        Task AddProctorSessionsAsync(IEnumerable<ProctorSession> proctorSessions);
        Task RemoveStudentSessionAsync(StudentSession studentSession);
        Task<List<StudentSession>> GetStudentSessionsByIdAsync(int examSessionId, int[] studentIds);
        Task UpdateAsync(ExamSession session);
        Task<ExamSession?> GetByIdWithProctorsAsync(int id);
        Task RemoveProctorSessionAsync(int examSessionId, int proctorId);
        Task<IEnumerable<ProctorSession>> GetProctorSessionsWithTimeConflictAsync(int proctorId, DateTime startTime, DateTime endTime);
        Task<IEnumerable<ExamSession>> GetByStatusAsync(ExamSessionStatus status, System.Linq.Expressions.Expression<System.Func<ExamSession, bool>>? predicate = null);
        Task<IEnumerable<ExamSession>> GetByStatusesAsync(ExamSessionStatus[] statuses, System.Linq.Expressions.Expression<System.Func<ExamSession, bool>>? predicate = null);
        Task<IEnumerable<WeeklyExamSessionStatsDto>> GetWeeklyStatisticsAsync();
        Task<int> CountActiveSessionsAsync();
        Task<int> CountStudentsInExamAsync();
        Task<IEnumerable<StudentSession>> GetStudentSessionsWithAlertsAsync(int examSessionId);
        Task<IEnumerable<StudentSession>> GetStudentSessionsWithTimeConflictAsync(IEnumerable<int> studentIds, DateTime startTime, DateTime endTime);

        /// <summary>
        /// Paginated sessions created by an admin, with key metrics.
        /// </summary>
        Task<(IReadOnlyList<dynamic> Sessions, int TotalCount)> GetAdminSessionsPagedAsync(int adminId, int page, int pageSize);
    }
}