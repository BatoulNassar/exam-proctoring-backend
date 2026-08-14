using ExamProctoring.Application.Common;
using ExamProctoring.Application.Common.DTOs;
using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Application.Features.ExamSessions.DTOs;
using ExamProctoring.Domain.Entities;
using ExamProctoring.Domain.Enums;
using System.Text;

namespace ExamProctoring.Application.Features.ExamSessions.Services
{
    public class ExamSessionService : IExamSessionService
    {
        private readonly IExamSessionRepository _examSessionRepository;
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IQuestionBankRepository _questionBankRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly IUserRepository _userRepository;
        private readonly IProctorSessionRepository _proctorSessionRepository;

        public ExamSessionService(
            IExamSessionRepository examSessionRepository,
            IAuditLogRepository auditLogRepository,
            IQuestionBankRepository questionBankRepository,
            IStudentRepository studentRepository,
            IUserRepository userRepository,
            IProctorSessionRepository proctorSessionRepository)
        {
            _examSessionRepository = examSessionRepository;
            _auditLogRepository = auditLogRepository;
            _questionBankRepository = questionBankRepository;
            _studentRepository = studentRepository;
            _userRepository = userRepository;
            _proctorSessionRepository = proctorSessionRepository;
        }

        public async Task<(CreateExamSessionResult Result, CreateExamSessionResponse? Response)> CreateSessionAsync(CreateExamSessionRequest request, int actorId)
        {
            var settingsAreValid = !string.IsNullOrWhiteSpace(request.Title)
                && !string.IsNullOrWhiteSpace(request.CourseCode)
                && request.DurationMinutes >= 1
                && request.GracePeriodMinutes >= 0
                && request.LoginWindowMinutes >= 0
                && request.EyeGazeThresholdSec >= 1;

            if (!settingsAreValid)
                return (CreateExamSessionResult.InvalidSettings, null);

            // The admin picks a day and a wall-clock time; both are anchored to
            // Damascus and folded into one UTC instant here, so everything stored and
            // every later comparison is in UTC.
            var scheduledStartUtc = ExamScheduleTime.ToUtc(request.ScheduledDate, request.ScheduledTime);

            if (scheduledStartUtc <= DateTime.UtcNow)
                return (CreateExamSessionResult.StartTimeInPast, null);

            if (!await _questionBankRepository.ExistsAsync(request.QuestionBankId))
                return (CreateExamSessionResult.QuestionBankNotFound, null);

            // Validate assigned proctors if provided
            var assignedProctorIds = new List<int>();
            if (request.AssignedProctorIds != null && request.AssignedProctorIds.Length > 0)
            {
                foreach (var proctorId in request.AssignedProctorIds.Distinct())
                {
                    var proctor = await _userRepository.GetByIdWithRolesAsync(proctorId);
                    if (proctor == null || !proctor.UserRoles.Any(ur => ur.Role.name == "Proctor"))
                        return (CreateExamSessionResult.InvalidProctor, null);

                    // Check if proctor is available for this time slot
                    // Compared in UTC, like the stored start times it is checked against.
                    var conflicts = await _examSessionRepository.GetProctorSessionsWithTimeConflictAsync(
                        proctorId, scheduledStartUtc, scheduledStartUtc.AddMinutes(request.DurationMinutes));
                    if (conflicts.Any())
                        return (CreateExamSessionResult.ProctorNotAvailable, null);

                    assignedProctorIds.Add(proctorId);
                }
            }

            List<string> csvNumbers = new();
            if (request.StudentsCsvFile != null)
            {
                var parsed = await ParseStudentsCsvAsync(request.StudentsCsvFile);
                if (parsed == null)
                    return (CreateExamSessionResult.InvalidCsv, null);
                csvNumbers = parsed;
            }

            var session = new ExamSession
            {
                title = request.Title.Trim(),
                course_tag = request.CourseCode.Trim(),
                status = ExamSessionStatus.DRAFT,
                start_time = scheduledStartUtc,
                duration_minutes = request.DurationMinutes,
                question_bank_id = request.QuestionBankId,
                grace_period_minutes = request.GracePeriodMinutes,
                login_window_minutes = request.LoginWindowMinutes,
                eye_gaze_threshold_sec = request.EyeGazeThresholdSec,
                face_alert_sensitivity = request.FaceAlertSensitivity,
                created_by_admin_id = actorId,
                created_at = DateTime.UtcNow,
                created_by = actorId,
            };
            await _examSessionRepository.AddAsync(session);

            // Assign proctors if provided
            if (assignedProctorIds.Any())
            {
                var proctorSessions = assignedProctorIds.Select(pId => new ProctorSession
                {
                    exam_session_id = session.id,
                    proctor_id = pId,
                    created_at = DateTime.UtcNow,
                    created_by = actorId,
                }).ToList();

                await _examSessionRepository.AddProctorSessionsAsync(proctorSessions);
            }

            var enrolledCount = 0;
            var unmatched = new List<string>();
            var conflicting = new List<string>();
            if (csvNumbers.Any())
            {
                var students = await _studentRepository.GetByUniversityNumbersAsync(csvNumbers);
                var matchedNumbers = students.Select(s => s.university_number).ToHashSet();
                unmatched = csvNumbers.Where(n => !matchedNumbers.Contains(n)).ToList();

                var sessionEnd = session.start_time.AddMinutes(session.duration_minutes);
                var conflicts = await _examSessionRepository.GetStudentSessionsWithTimeConflictAsync(
                    students.Select(s => s.id), session.start_time, sessionEnd);
                var conflictingStudentIds = conflicts.Select(c => c.student_id).ToHashSet();

                conflicting = students
                    .Where(s => conflictingStudentIds.Contains(s.id))
                    .Select(s => s.university_number)
                    .ToList();

                var studentsToEnroll = students.Where(s => !conflictingStudentIds.Contains(s.id)).ToList();
                var studentSessions = studentsToEnroll.Select(s => new StudentSession
                {
                    exam_session_id = session.id,
                    student_id = s.id,
                    status = StudentSessionStatus.NotStarted,
                    created_at = DateTime.UtcNow,
                    created_by = actorId,
                }).ToList();

                if (studentSessions.Any())
                    await _examSessionRepository.AddStudentSessionsAsync(studentSessions);
                enrolledCount = studentSessions.Count;
            }

            var details = await GetSessionDetailsAsync(session.id);
            return (CreateExamSessionResult.Created, new CreateExamSessionResponse
            {
                Session = details!,
                EnrolledStudentsCount = enrolledCount,
                UnmatchedUniversityNumbers = unmatched,
                ConflictingStudentNumbers = conflicting,
            });
        }

        /// <summary>
        /// Parses a roster CSV ("university_number,full_name", header optional).
        /// Returns the distinct university numbers, or null when the file has no usable rows.
        /// </summary>
        private static async Task<List<string>?> ParseStudentsCsvAsync(Microsoft.AspNetCore.Http.IFormFile file)
        {
            var numbers = new List<string>();

            using var reader = new StreamReader(file.OpenReadStream());
            string? line;
            var isFirstLine = true;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = line.Split(',');
                if (parts.Length == 0) continue;

                var universityNumber = parts[0].Trim(' ', '"', '\'');

                if (isFirstLine)
                {
                    isFirstLine = false;
                    if (universityNumber.Equals("university_number", StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                if (!string.IsNullOrWhiteSpace(universityNumber))
                    numbers.Add(universityNumber);
            }

            return numbers.Any() ? numbers.Distinct().ToList() : null;
        }

        public async Task<PagedResult<ExamSessionDto>> GetAllSessionsAsync(int page, int pageSize, int? adminId = null)
        {
            var sessions = await _examSessionRepository.GetAllSessionsAsync(page, pageSize, adminId);
            var totalCount = await _examSessionRepository.CountAllSessionsAsync(adminId);

            var items = sessions.Select(s => new ExamSessionDto
            {
                Id = s.id,
                Title = s.title,
                CourseTag = s.course_tag,
                Status = s.status.ToString(),
                // Stored in UTC, sent out carrying Damascus's offset, so a client can
                // print it as-is and still parse it to the right instant.
                StartTime = ExamScheduleTime.ToLocalOffset(s.start_time),
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
                UpdatedBy = s.updated_by,
                StudentCount = s.StudentSessions?.Count ?? 0,
                ProctorName = s.ProctorSessions?.FirstOrDefault()?.Proctor?.full_name ?? "N/A"
            }).ToList();

            return new PagedResult<ExamSessionDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<ExamSessionDetailsDto?> GetSessionDetailsAsync(int id, int? adminId = null)
        {
            var session = await _examSessionRepository.GetByIdWithProctorsAsync(id);

            if (session == null)
                return null;

            // Treated as not found rather than forbidden: an admin should not be able
            // to learn that another admin's session exists by probing ids.
            if (adminId.HasValue && session.created_by_admin_id != adminId.Value)
                return null;

            var assignedProctors = session.ProctorSessions?
                .Select(ps => new AvailableProctorDto
                {
                    Id = ps.proctor_id,
                    FullName = ps.Proctor?.full_name ?? string.Empty,
                    Email = ps.Proctor?.email ?? string.Empty,
                })
                .ToList() ?? new List<AvailableProctorDto>();

            // Get question bank separately if needed for details
            var sessionWithBank = await _examSessionRepository.GetByIdWithDetailsAsync(id);

            // Get enrolled students
            var enrolledStudents = session.StudentSessions?
                .Select(ss => new EnrolledStudentDto
                {
                    Id = ss.student_id,
                    UniversityNumber = ss.Student?.university_number ?? string.Empty,
                    FullName = $"{ss.Student?.first_name} {ss.Student?.last_name}".Trim(),
                    Email = ss.Student?.email ?? string.Empty,
                })
                .ToList() ?? new List<EnrolledStudentDto>();

            return new ExamSessionDetailsDto
            {
                Id = session.id,
                Title = session.title,
                CourseTag = session.course_tag,
                Status = session.status.ToString(),
                StartTime = ExamScheduleTime.ToLocalOffset(session.start_time),
                DurationMinutes = session.duration_minutes,
                GracePeriodMinutes = session.grace_period_minutes,
                LoginWindowMinutes = session.login_window_minutes,
                AssignedProctors = assignedProctors,
                EnrolledStudents = enrolledStudents,
                QuestionBankId = session.question_bank_id,
                QuestionBankTitle = sessionWithBank?.QuestionBank?.title ?? "N/A",
                QuestionCount = sessionWithBank?.QuestionBank?.Questions?.Count ?? 0,
                Randomization = sessionWithBank?.QuestionBank?.randomization ?? false,
                OptionShuffle = sessionWithBank?.QuestionBank?.option_shuffle ?? false,
                EyeGazeThresholdSec = session.eye_gaze_threshold_sec,
                FaceAlertSensitivity = session.face_alert_sensitivity.ToString(),
                LockedAt = session.locked_at,
                ClosedAt = session.closed_at,
                CreatedAt = session.created_at,
            };
        }

        public async Task<DeleteExamSessionResult> DeleteSessionAsync(int id, int actorId)
        {
            var session = await _examSessionRepository.GetByIdAsync(id);

            if (session == null)
                return DeleteExamSessionResult.NotFound;

            if (session.status != ExamSessionStatus.DRAFT)
                return DeleteExamSessionResult.NotDraft;

            session.is_deleted = true;
            session.deleted_at = DateTime.UtcNow;
            session.deleted_by = actorId;
            await _examSessionRepository.UpdateAsync(session);

            return DeleteExamSessionResult.Deleted;
        }

        public async Task<(UpdateExamSessionResult Result, ExamSessionDetailsDto? Session)> UpdateSessionAsync(int id, UpdateExamSessionRequest request, int actorId)
        {
            var session = await _examSessionRepository.GetByIdAsync(id);

            if (session == null)
                return (UpdateExamSessionResult.NotFound, null);

            // SCHEDULED state: only allow proctor updates (edit restore)
            if (session.status == ExamSessionStatus.SCHEDULED)
            {
                if (request.AssignedProctorIds != null)
                {
                    // Validate and update proctors
                    foreach (var proctorId in request.AssignedProctorIds.Distinct())
                    {
                        var proctor = await _userRepository.GetByIdWithRolesAsync(proctorId);
                        if (proctor == null || !proctor.UserRoles.Any(ur => ur.Role.name == "Proctor"))
                            return (UpdateExamSessionResult.InvalidSettings, null);

                        var conflicts = await _examSessionRepository.GetProctorSessionsWithTimeConflictAsync(
                            proctorId, session.start_time, session.start_time.AddMinutes(session.duration_minutes));

                        // Exclude current session from conflict check
                        if (conflicts.Any(cs => cs.id != id))
                            return (UpdateExamSessionResult.InvalidSettings, null);
                    }

                    // Remove existing proctors and add new ones
                    var existingProctors = await _proctorSessionRepository.GetByExamSessionIdAsync(id);
                    foreach (var existing in existingProctors)
                    {
                        await _proctorSessionRepository.DeleteAsync(existing);
                    }

                    // Add new proctors
                    var newProctorSessions = request.AssignedProctorIds.Distinct().Select(pId => new ProctorSession
                    {
                        exam_session_id = session.id,
                        proctor_id = pId,
                        created_at = DateTime.UtcNow,
                        created_by = actorId,
                    }).ToList();

                    if (newProctorSessions.Any())
                        await _examSessionRepository.AddProctorSessionsAsync(newProctorSessions);
                }

                session.updated_at = DateTime.UtcNow;
                session.updated_by = actorId;
                await _examSessionRepository.UpdateAsync(session);

                var details = await GetSessionDetailsAsync(id);
                return (UpdateExamSessionResult.Updated, details);
            }

            // DRAFT state: allow all updates
            if (session.status != ExamSessionStatus.DRAFT)
                return (UpdateExamSessionResult.NotDraft, null);

            var settingsAreValid = (request.Title == null || !string.IsNullOrWhiteSpace(request.Title))
                && (request.CourseTag == null || !string.IsNullOrWhiteSpace(request.CourseTag))
                && (request.DurationMinutes == null || request.DurationMinutes >= 1)
                && (request.GracePeriodMinutes == null || request.GracePeriodMinutes >= 0)
                && (request.LoginWindowMinutes == null || request.LoginWindowMinutes >= 0)
                && (request.EyeGazeThresholdSec == null || request.EyeGazeThresholdSec >= 1);

            if (!settingsAreValid)
                return (UpdateExamSessionResult.InvalidSettings, null);

            if (request.QuestionBankId.HasValue
                && !await _questionBankRepository.ExistsAsync(request.QuestionBankId.Value))
                return (UpdateExamSessionResult.QuestionBankNotFound, null);

            if (request.Title != null) session.title = request.Title.Trim();
            if (request.CourseTag != null) session.course_tag = request.CourseTag.Trim();
            // Date and time move together: accepting one alone would silently pair a
            // new day with the old clock time.
            if (request.StartDate.HasValue != request.StartTimeOfDay.HasValue)
                return (UpdateExamSessionResult.InvalidSettings, null);

            if (request.StartDate.HasValue && request.StartTimeOfDay.HasValue)
                session.start_time = ExamScheduleTime.ToUtc(request.StartDate.Value, request.StartTimeOfDay.Value);
            if (request.DurationMinutes.HasValue) session.duration_minutes = request.DurationMinutes.Value;
            if (request.QuestionBankId.HasValue) session.question_bank_id = request.QuestionBankId.Value;
            if (request.GracePeriodMinutes.HasValue) session.grace_period_minutes = request.GracePeriodMinutes.Value;
            if (request.LoginWindowMinutes.HasValue) session.login_window_minutes = request.LoginWindowMinutes.Value;
            if (request.EyeGazeThresholdSec.HasValue) session.eye_gaze_threshold_sec = request.EyeGazeThresholdSec.Value;
            if (request.FaceAlertSensitivity.HasValue) session.face_alert_sensitivity = request.FaceAlertSensitivity.Value;

            // Handle proctor updates in DRAFT
            if (request.AssignedProctorIds != null)
            {
                foreach (var proctorId in request.AssignedProctorIds.Distinct())
                {
                    var proctor = await _userRepository.GetByIdWithRolesAsync(proctorId);
                    if (proctor == null || !proctor.UserRoles.Any(ur => ur.Role.name == "Proctor"))
                        return (UpdateExamSessionResult.InvalidSettings, null);

                    var conflicts = await _examSessionRepository.GetProctorSessionsWithTimeConflictAsync(
                        proctorId, session.start_time, session.start_time.AddMinutes(session.duration_minutes));
                    if (conflicts.Any(cs => cs.id != id))
                        return (UpdateExamSessionResult.InvalidSettings, null);
                }

                var existingProctors = await _proctorSessionRepository.GetByExamSessionIdAsync(id);
                foreach (var existing in existingProctors)
                {
                    await _proctorSessionRepository.DeleteAsync(existing);
                }

                var newProctorSessions = request.AssignedProctorIds.Distinct().Select(pId => new ProctorSession
                {
                    exam_session_id = session.id,
                    proctor_id = pId,
                    created_at = DateTime.UtcNow,
                    created_by = actorId,
                }).ToList();

                if (newProctorSessions.Any())
                    await _examSessionRepository.AddProctorSessionsAsync(newProctorSessions);
            }

            session.updated_at = DateTime.UtcNow;
            session.updated_by = actorId;
            await _examSessionRepository.UpdateAsync(session);

            var detailsDto = await GetSessionDetailsAsync(id);
            return (UpdateExamSessionResult.Updated, detailsDto);
        }

        public async Task<(EditRestoreSessionResult Result, ExamSessionDetailsDto? Session)> EditRestoreSessionAsync(int id, EditRestoreSessionRequest request, int actorId)
        {
            var session = await _examSessionRepository.GetByIdAsync(id);

            if (session == null)
                return (EditRestoreSessionResult.NotFound, null);

            if (session.status != ExamSessionStatus.SCHEDULED)
                return (EditRestoreSessionResult.NotScheduled, null);

            // Validate and assign proctors if provided
            if (request.AssignedProctorIds != null && request.AssignedProctorIds.Length > 0)
            {
                foreach (var proctorId in request.AssignedProctorIds.Distinct())
                {
                    var proctor = await _userRepository.GetByIdWithRolesAsync(proctorId);
                    if (proctor == null || !proctor.UserRoles.Any(ur => ur.Role.name == "Proctor"))
                        return (EditRestoreSessionResult.InvalidProctor, null);

                    var conflicts = await _examSessionRepository.GetProctorSessionsWithTimeConflictAsync(
                        proctorId, session.start_time, session.start_time.AddMinutes(session.duration_minutes));
                    if (conflicts.Any(cs => cs.exam_session_id != id))
                        return (EditRestoreSessionResult.ProctorNotAvailable, null);
                }

                // Remove existing proctors and add new ones
                var existingProctors = await _proctorSessionRepository.GetByExamSessionIdAsync(id);
                foreach (var existing in existingProctors)
                {
                    await _proctorSessionRepository.DeleteAsync(existing);
                }

                var newProctorSessions = request.AssignedProctorIds.Distinct().Select(pId => new ProctorSession
                {
                    exam_session_id = id,
                    proctor_id = pId,
                    created_at = DateTime.UtcNow,
                    created_by = actorId,
                }).ToList();

                if (newProctorSessions.Any())
                    await _examSessionRepository.AddProctorSessionsAsync(newProctorSessions);
            }

            // Add students if provided
            if (request.StudentIdsToAdd != null && request.StudentIdsToAdd.Length > 0)
            {
                var studentsToAdd = await _studentRepository.GetByIdsAsync(request.StudentIdsToAdd);
                if (studentsToAdd.Count != request.StudentIdsToAdd.Length)
                    return (EditRestoreSessionResult.InvalidStudent, null);

                var sessionEnd = session.start_time.AddMinutes(session.duration_minutes);
                var conflicts = await _examSessionRepository.GetStudentSessionsWithTimeConflictAsync(
                    studentsToAdd.Select(s => s.id), session.start_time, sessionEnd);
                var conflictingIds = conflicts.Select(c => c.student_id).ToHashSet();

                var newStudentSessions = studentsToAdd
                    .Where(s => !conflictingIds.Contains(s.id))
                    .Select(s => new StudentSession
                    {
                        exam_session_id = id,
                        student_id = s.id,
                        status = StudentSessionStatus.NotStarted,
                        created_at = DateTime.UtcNow,
                        created_by = actorId,
                    }).ToList();

                if (newStudentSessions.Any())
                    await _examSessionRepository.AddStudentSessionsAsync(newStudentSessions);
            }

            // Remove students if provided
            if (request.StudentIdsToRemove != null && request.StudentIdsToRemove.Length > 0)
            {
                var sessionsToRemove = await _examSessionRepository.GetStudentSessionsByIdAsync(id, request.StudentIdsToRemove);
                foreach (var studentSession in sessionsToRemove)
                {
                    await _examSessionRepository.RemoveStudentSessionAsync(studentSession);
                }
            }

            session.updated_at = DateTime.UtcNow;
            session.updated_by = actorId;
            await _examSessionRepository.UpdateAsync(session);

            var details = await GetSessionDetailsAsync(id);
            return (EditRestoreSessionResult.Updated, details);
        }

        public async Task<PublishExamSessionResult> PublishSessionAsync(int id)
        {
            var session = await _examSessionRepository.GetByIdWithQuestionBankAsync(id);

            if (session == null)
                return PublishExamSessionResult.NotFound;

            if (session.status != ExamSessionStatus.DRAFT)
                return PublishExamSessionResult.NotDraft;

            if (session.start_time <= DateTime.UtcNow)
                return PublishExamSessionResult.StartTimeInPast;

            var bankIsReady = session.QuestionBank != null
                && session.QuestionBank.status != QuestionBankStatus.Archived
                && session.QuestionBank.Questions.Any();

            if (!bankIsReady)
                return PublishExamSessionResult.QuestionBankNotReady;

            session.status = ExamSessionStatus.SCHEDULED;
            session.updated_at = DateTime.UtcNow;
            await _examSessionRepository.UpdateAsync(session);

            return PublishExamSessionResult.Published;
        }

        public async Task<ExtendSessionTimeResult> ExtendSessionTimeAsync(int id, int extraMinutes, int actorId)
        {
            if (extraMinutes < 1 || extraMinutes > 120)
                return ExtendSessionTimeResult.InvalidExtraMinutes;

            var session = await _examSessionRepository.GetByIdAsync(id);

            if (session == null)
                return ExtendSessionTimeResult.NotFound;

            if (session.status != ExamSessionStatus.ACTIVE)
                return ExtendSessionTimeResult.NotActive;

            session.duration_minutes += extraMinutes;
            session.updated_at = DateTime.UtcNow;
            session.updated_by = actorId;
            await _examSessionRepository.UpdateAsync(session);

            await _auditLogRepository.AddAsync(new AuditLog
            {
                exam_session_id = session.id,
                actor_id = actorId,
                actor_type = "SuperAdmin",
                action = "SessionTimeExtended",
                entity_id = session.id,
                entity_type = "ExamSession",
                details = $"Super Admin override: extended \"{session.title}\" by {extraMinutes} minutes (new duration: {session.duration_minutes} min)",
                occurred_at = DateTime.UtcNow,
                created_at = DateTime.UtcNow,
                created_by = actorId,
            });

            return ExtendSessionTimeResult.Extended;
        }

        public async Task<IEnumerable<AvailableProctorDto>> GetAvailableProctorsAsync(DateTime sessionStart, DateTime sessionEnd)
        {
            // Get all users with Proctor role
            var allProctors = await _userRepository.GetByRoleAsync("Proctor");

            var availableProctors = new List<AvailableProctorDto>();

            foreach (var proctor in allProctors)
            {
                // Check if proctor has any conflicting sessions
                var conflicts = await _examSessionRepository.GetProctorSessionsWithTimeConflictAsync(
                    proctor.id, sessionStart, sessionEnd);

                if (!conflicts.Any())
                {
                    availableProctors.Add(new AvailableProctorDto
                    {
                        Id = proctor.id,
                        FullName = proctor.full_name,
                        Email = proctor.email,
                    });
                }
            }

            return availableProctors;
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

        public async Task<string?> ExportGradingReportAsync(int sessionId)
        {
            var session = await _examSessionRepository.GetByIdWithProctorsAsync(sessionId);

            if (session == null)
                return null;

            if (session.status != ExamSessionStatus.CLOSED && session.status != ExamSessionStatus.ARCHIVED)
                return string.Empty;

            var csv = new StringBuilder();

            csv.AppendLine("Session Export Report");
            csv.AppendLine($"Session Title,{CsvEscape(session.title)}");
            csv.AppendLine($"Course Code,{CsvEscape(session.course_tag)}");
            csv.AppendLine($"Status,{session.status}");
            csv.AppendLine($"Start Time,{session.start_time:yyyy-MM-dd HH:mm:ss}");
            csv.AppendLine($"Duration (Minutes),{session.duration_minutes}");
            csv.AppendLine();

            csv.AppendLine("Student ID,University Number,Full Name,Email");
            if (session.StudentSessions != null)
            {
                foreach (var ss in session.StudentSessions)
                {
                    var fullName = $"{ss.Student?.first_name ?? string.Empty} {ss.Student?.last_name ?? string.Empty}";
                    csv.AppendLine($"{ss.student_id},{CsvEscape(ss.Student?.university_number ?? string.Empty)},{CsvEscape(fullName)},{CsvEscape(ss.Student?.email ?? string.Empty)}");
                }
            }

            return csv.ToString();
        }

        public async Task<string?> ExportAuditLogAsync(int sessionId)
        {
            var session = await _examSessionRepository.GetByIdAsync(sessionId);

            if (session == null)
                return null;

            if (session.status != ExamSessionStatus.CLOSED && session.status != ExamSessionStatus.ARCHIVED)
                return string.Empty;

            var auditLogs = await _auditLogRepository.GetByExamSessionIdAsync(sessionId);

            var csv = new StringBuilder();

            csv.AppendLine("Audit Log Export");
            csv.AppendLine($"Session: {CsvEscape(session.title)}");
            csv.AppendLine($"Exported: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
            csv.AppendLine();

            csv.AppendLine("Timestamp,Actor,Action,Entity Type,Details");
            foreach (var log in auditLogs)
            {
                csv.AppendLine($"{log.occurred_at:yyyy-MM-dd HH:mm:ss},{CsvEscape(log.actor_id.ToString())},{CsvEscape(log.action)},{CsvEscape(log.entity_type)},{CsvEscape(log.details ?? string.Empty)}");
            }

            return csv.ToString();
        }

        private static string CsvEscape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }

            return value;
        }
    }
}