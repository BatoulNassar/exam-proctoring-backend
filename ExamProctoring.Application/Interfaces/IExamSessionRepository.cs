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
        Task<IEnumerable<ExamSession>> GetAllSessionsAsync(int page, int pageSize);
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
    }
}