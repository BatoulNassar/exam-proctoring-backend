using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Application.Features.Dashboard.DTOs;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Features.Dashboard.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IExamSessionRepository _examSessionRepository;
        private readonly IAlertRepository _alertRepository;
        private readonly IQuestionBankRepository _questionBankRepository;
        private readonly IUserRepository _userRepository;

        public DashboardService(IExamSessionRepository examSessionRepository,IAlertRepository alertRepository,IQuestionBankRepository questionBankRepository,IUserRepository userRepository)
        {
            _examSessionRepository = examSessionRepository;
            _alertRepository = alertRepository;
            _questionBankRepository = questionBankRepository;
            _userRepository = userRepository;
        }

        public async Task<DashboardStatsDto> GetStatsAsync()
        {
            return new DashboardStatsDto
            {
                ActiveSessions = await _examSessionRepository.CountActiveSessionsAsync(),
                StudentsInExam = await _examSessionRepository.CountStudentsInExamAsync(),
                OpenAlerts = await _alertRepository.CountOpenAlertsAsync(),
                QuestionBanks = await _questionBankRepository.CountAsync(),
                AdminUsers = await _userRepository.CountAdminsAsync(),
            };
        }
    }
}
