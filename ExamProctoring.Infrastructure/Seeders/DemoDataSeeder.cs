using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Domain.Entities;
using ExamProctoring.Domain.Enums;
using ExamProctoring.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ExamProctoring.Infrastructure.Seeders
{
    /// <summary>
    /// Seeds demo data for the main tables: admins, students, question banks,
    /// questions, exam sessions (one per status), student sessions, monitoring
    /// events, alerts, proctor actions, warning messages and audit logs.
    /// Safe to run multiple times: skips seeding if exam sessions already exist.
    /// </summary>
    public class DemoDataSeeder
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        public DemoDataSeeder(AppDbContext context, IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task SeedAsync()
        {
            if (await _context.ExamSessions.AnyAsync()) return;

            var now = DateTime.UtcNow;

            var admins = await SeedAdminsAsync(now);
            var proctors = await SeedProctorsAsync(now);
            var students = await SeedStudentsAsync(now);
            var banks = await SeedQuestionBanksAsync(admins, now);
            await SeedQuestionsAsync(banks, now);
            var sessions = await SeedExamSessionsAsync(admins, banks, now);
            await SeedProctorSessionsAsync(sessions, proctors, now);
            var studentSessions = await SeedStudentSessionsAsync(sessions, students, now);
            await SeedMonitoringAndAlertsAsync(studentSessions, admins, now);
            await SeedAuditLogsAsync(sessions, admins, now);
        }

        private async Task<List<User>> SeedAdminsAsync(DateTime now)
        {
            var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.name == "Admin");
            if (adminRole == null)
            {
                adminRole = new Role { name = "Admin", created_at = now };
                await _context.Roles.AddAsync(adminRole);
                await _context.SaveChangesAsync();
            }

            var adminUsernames = new List<string> { "d.ahmad", "e.sarah", "d.rania", "d.ali" };

            var existingAdmins = await _context.Users
                .Where(u => adminUsernames.Contains(u.user_name))
                .ToListAsync();

            if (existingAdmins.Count == adminUsernames.Count)
            {
                return existingAdmins;
            }

            var newAdmins = new List<User>();
            var defaultAdmins = new List<User>
    {
        new User { user_name = "d.ahmad",  full_name = "Dr. Ahmad Hassan",  email = "d.ahmad@vu.edu",  phone_number = "0930000001" },
        new User { user_name = "e.sarah",  full_name = "Eng. Sarah Khalil", email = "e.sarah@vu.edu",  phone_number = "0930000002" },
        new User { user_name = "d.rania",  full_name = "Dr. Rania Yousef",  email = "d.rania@vu.edu",  phone_number = "0930000003" },
        new User { user_name = "d.ali",    full_name = "Dr. Ali Nasser",    email = "d.ali@vu.edu",    phone_number = "0930000004" },
    };

            foreach (var admin in defaultAdmins)
            {
                if (!existingAdmins.Any(u => u.user_name == admin.user_name))
                {
                    admin.password_hash = _passwordHasher.Hash("Admin123!");
                    admin.created_at = now;
                    newAdmins.Add(admin);
                }
            }

            if (newAdmins.Any())
            {
                await _context.Users.AddRangeAsync(newAdmins);
                await _context.SaveChangesAsync();

                var userRoles = newAdmins.Select(a => new User_Roles
                {
                    user_id = a.id,
                    role_id = adminRole.id,
                    created_at = now
                });
                await _context.UserRoles.AddRangeAsync(userRoles);
                await _context.SaveChangesAsync();
            }

            return existingAdmins.Concat(newAdmins).ToList();
        }

        private async Task<List<User>> SeedProctorsAsync(DateTime now)
        {
            var proctorRole = await _context.Roles.FirstOrDefaultAsync(r => r.name == "Proctor");
            if (proctorRole == null)
            {
                proctorRole = new Role { name = "Proctor", created_at = now };
                await _context.Roles.AddAsync(proctorRole);
                await _context.SaveChangesAsync();
            }

            var proctorUsernames = new List<string> { "p.farah", "p.amira" };

            var existingProctors = await _context.Users
                .Where(u => proctorUsernames.Contains(u.user_name))
                .ToListAsync();

            if (existingProctors.Count == proctorUsernames.Count)
            {
                return existingProctors;
            }

            var defaultProctors = new List<User>
            {
                new User { user_name = "p.farah", full_name = "Dr. Farah Nazari", email = "p.farah@vu.edu", phone_number = "0930000005" },
                new User { user_name = "p.amira", full_name = "Eng. Amira Khalil", email = "p.amira@vu.edu", phone_number = "0930000006" },
            };

            var newProctors = new List<User>();
            foreach (var proctor in defaultProctors)
            {
                if (!existingProctors.Any(u => u.user_name == proctor.user_name))
                {
                    proctor.password_hash = _passwordHasher.Hash("Proctor123!");
                    proctor.created_at = now;
                    newProctors.Add(proctor);
                }
            }

            if (newProctors.Any())
            {
                await _context.Users.AddRangeAsync(newProctors);
                await _context.SaveChangesAsync();

                var userRoles = newProctors.Select(p => new User_Roles
                {
                    user_id = p.id,
                    role_id = proctorRole.id,
                    created_at = now
                });
                await _context.UserRoles.AddRangeAsync(userRoles);
                await _context.SaveChangesAsync();
            }

            return existingProctors.Concat(newProctors).ToList();
        }

        private async Task<List<Student>> SeedStudentsAsync(DateTime now)
        {
            var studentUsernames = new List<string>
    {
        "layla.mansour", "karim.nasir", "omar.khalil", "nour.haddad",
        "yara.saleh", "hadi.aziz", "rima.awad", "tarek.salem"
    };

            var existingStudents = await _context.Students
                .Where(s => studentUsernames.Contains(s.user_name))
                .ToListAsync();

            var defaultStudents = new List<Student>
    {
        new Student { first_name = "Layla",  middle_name = "Sami",  last_name = "Mansour", university_number = "S-20211847", user_name = "layla.mansour", email = "layla.mansour@student.vu.edu",  phone_number = "0940000001", face_id = "face_layla_20211847" },
        new Student { first_name = "Karim",  middle_name = "Adel",  last_name = "Nasir",   university_number = "S-20211285", user_name = "karim.nasir",   email = "karim.nasir@student.vu.edu",    phone_number = "0940000002", face_id = "face_karim_20211285" },
        new Student { first_name = "Omar",   middle_name = "Fadi",  last_name = "Khalil",  university_number = "S-20210934", user_name = "omar.khalil",   email = "omar.khalil@student.vu.edu",    phone_number = "0940000003", face_id = "face_omar_20210934" },
        new Student { first_name = "Nour",   middle_name = "Hani",  last_name = "Haddad",  university_number = "S-20212210", user_name = "nour.haddad",   email = "nour.haddad@student.vu.edu",    phone_number = "0940000004", face_id = "face_nour_20212210" },
        new Student { first_name = "Yara",   middle_name = "Ziad",  last_name = "Saleh",   university_number = "S-20213321", user_name = "yara.saleh",    email = "yara.saleh@student.vu.edu",     phone_number = "0940000005", face_id = "face_yara_20213321" },
        new Student { first_name = "Hadi",   middle_name = "Samer", last_name = "Aziz",    university_number = "S-20214412", user_name = "hadi.aziz",     email = "hadi.aziz@student.vu.edu",      phone_number = "0940000006", face_id = "face_hadi_20214412" },
        new Student { first_name = "Rima",   middle_name = "Nabil", last_name = "Awad",    university_number = "S-20215503", user_name = "rima.awad",     email = "rima.awad@student.vu.edu",      phone_number = "0940000007", face_id = "face_rima_20215503" },
        new Student { first_name = "Tarek",  middle_name = "Walid", last_name = "Salem",   university_number = "S-20216604", user_name = "tarek.salem",   email = "tarek.salem@student.vu.edu",    phone_number = "0940000008", face_id = "face_tarek_20216604" }
    };

            var newStudents = new List<Student>();
            foreach (var student in defaultStudents)
            {
                if (!existingStudents.Any(s => s.user_name == student.user_name))
                {
                    student.password = _passwordHasher.Hash("Student123!");
                    student.created_at = now;
                    newStudents.Add(student);
                }
            }
            if (newStudents.Any())
            {
                await _context.Students.AddRangeAsync(newStudents);
                await _context.SaveChangesAsync();
            }

            var allStudents = existingStudents.Concat(newStudents).ToList();
            return allStudents.OrderBy(s => studentUsernames.IndexOf(s.user_name)).ToList();
        }
        private async Task<List<QuestionBank>> SeedQuestionBanksAsync(List<User> admins, DateTime now)
        {
            var banks = new List<QuestionBank>
            {
                new QuestionBank { title = "CS101 Question Bank",    course_code = "CS101",   status = QuestionBankStatus.Published, version = "1.2", authored_by_admin_id = admins[0].id, locked_at = now.AddDays(-10), randomization = true,  option_shuffle = true,  created_at = now.AddDays(-30) },
                new QuestionBank { title = "MATH202 Question Bank",  course_code = "MATH202", status = QuestionBankStatus.Published, version = "1.0", authored_by_admin_id = admins[1].id, locked_at = now.AddDays(-5),  randomization = true,  option_shuffle = false, created_at = now.AddDays(-25) },
                new QuestionBank { title = "CS201 Question Bank",    course_code = "CS201",   status = QuestionBankStatus.Published, version = "2.1", authored_by_admin_id = admins[3].id, locked_at = now.AddMonths(-14), randomization = false, option_shuffle = true, created_at = now.AddMonths(-15) },
                new QuestionBank { title = "DB202 Question Bank",    course_code = "DB202",   status = QuestionBankStatus.Draft,     version = "0.3", authored_by_admin_id = admins[2].id, locked_at = null,             randomization = true,  option_shuffle = true,  created_at = now.AddDays(-3) },
            };

            await _context.QuestionBanks.AddRangeAsync(banks);
            await _context.SaveChangesAsync();
            return banks;
        }

        private async Task SeedQuestionsAsync(List<QuestionBank> banks, DateTime now)
        {
            var questions = new List<Question>
            {
                // CS101
                new Question { question_bank_id = banks[0].id, type = QuestionType.MultipleChoice, question_text = "Which data structure uses FIFO ordering?", option_a = "Stack", option_b = "Queue", option_c = "Tree", option_d = "Graph", correct_answer = "B", marks = 2, created_at = now },
                new Question { question_bank_id = banks[0].id, type = QuestionType.TrueFalse,      question_text = "A binary search requires a sorted array.", correct_answer = "True", marks = 1, created_at = now },
                new Question { question_bank_id = banks[0].id, type = QuestionType.ShortAnswer,    question_text = "What does CPU stand for?", correct_answer = "Central Processing Unit", marks = 2, created_at = now },
                new Question { question_bank_id = banks[0].id, type = QuestionType.MultipleChoice, question_text = "What is the time complexity of binary search?", option_a = "O(n)", option_b = "O(n log n)", option_c = "O(log n)", option_d = "O(1)", correct_answer = "C", marks = 2, created_at = now },

                // MATH202
                new Question { question_bank_id = banks[1].id, type = QuestionType.MultipleChoice, question_text = "What is the derivative of x^2?", option_a = "x", option_b = "2x", option_c = "x^2", option_d = "2", correct_answer = "B", marks = 2, created_at = now },
                new Question { question_bank_id = banks[1].id, type = QuestionType.TrueFalse,      question_text = "The integral of a constant is zero.", correct_answer = "False", marks = 1, created_at = now },
                new Question { question_bank_id = banks[1].id, type = QuestionType.ShortAnswer,    question_text = "State the limit of sin(x)/x as x approaches 0.", correct_answer = "1", marks = 3, created_at = now },

                // CS201
                new Question { question_bank_id = banks[2].id, type = QuestionType.MultipleChoice, question_text = "Which keyword defines a class in C#?", option_a = "struct", option_b = "class", option_c = "interface", option_d = "enum", correct_answer = "B", marks = 1, created_at = now },
                new Question { question_bank_id = banks[2].id, type = QuestionType.TrueFalse,      question_text = "C# supports multiple class inheritance.", correct_answer = "False", marks = 1, created_at = now },
                new Question { question_bank_id = banks[2].id, type = QuestionType.ShortAnswer,    question_text = "Name the base class of all types in .NET.", correct_answer = "System.Object", marks = 2, created_at = now },

                // DB202
                new Question { question_bank_id = banks[3].id, type = QuestionType.MultipleChoice, question_text = "Which SQL clause filters grouped rows?", option_a = "WHERE", option_b = "GROUP BY", option_c = "HAVING", option_d = "ORDER BY", correct_answer = "C", marks = 2, created_at = now },
                new Question { question_bank_id = banks[3].id, type = QuestionType.TrueFalse,      question_text = "A primary key can contain NULL values.", correct_answer = "False", marks = 1, created_at = now },
            };

            await _context.Questions.AddRangeAsync(questions);
            await _context.SaveChangesAsync();
        }

        private async Task<List<ExamSession>> SeedExamSessionsAsync(List<User> admins, List<QuestionBank> banks, DateTime now)
        {
            var sessions = new List<ExamSession>
            {
                // One session per lifecycle status
                new ExamSession { title = "CS101 Intro Quiz",        course_tag = "CS101",   status = ExamSessionStatus.DRAFT,     start_time = now.AddDays(8),     duration_minutes = 30,  question_bank_id = banks[0].id, grace_period_minutes = 5,  login_window_minutes = 15, eye_gaze_threshold_sec = 3, created_by_admin_id = admins[0].id, created_at = now.AddDays(-2) },
                new ExamSession { title = "AI501 Final Project",     course_tag = "AI501",   status = ExamSessionStatus.SCHEDULED, start_time = now.AddDays(3),     duration_minutes = 120, question_bank_id = banks[1].id, grace_period_minutes = 10, login_window_minutes = 20, eye_gaze_threshold_sec = 3, created_by_admin_id = admins[1].id, created_at = now.AddDays(-7) },
                new ExamSession { title = "NET410 Quiz 3",           course_tag = "NET410",  status = ExamSessionStatus.LOCKED,    start_time = now.AddHours(6),    duration_minutes = 45,  question_bank_id = banks[0].id, grace_period_minutes = 5,  login_window_minutes = 10, eye_gaze_threshold_sec = 3, created_by_admin_id = admins[2].id, locked_at = now.AddHours(-2), created_at = now.AddDays(-5) },
                new ExamSession { title = "Midterm Exam - CS101",    course_tag = "CS101",   status = ExamSessionStatus.ACTIVE,    start_time = now.AddMinutes(-40), duration_minutes = 90, question_bank_id = banks[0].id, grace_period_minutes = 10, login_window_minutes = 15, eye_gaze_threshold_sec = 3, created_by_admin_id = admins[0].id, locked_at = now.AddHours(-3), created_at = now.AddDays(-10) },
                new ExamSession { title = "Final Quiz - Math202",    course_tag = "MATH202", status = ExamSessionStatus.GRACE,     start_time = now.AddMinutes(-70), duration_minutes = 60, question_bank_id = banks[1].id, grace_period_minutes = 15, login_window_minutes = 10, eye_gaze_threshold_sec = 3, created_by_admin_id = admins[1].id, locked_at = now.AddHours(-4), created_at = now.AddDays(-12) },
                new ExamSession { title = "DB202 Midterm",           course_tag = "DB202",   status = ExamSessionStatus.CLOSED,    start_time = now.AddDays(-19),   duration_minutes = 60,  question_bank_id = banks[3].id, grace_period_minutes = 10, login_window_minutes = 15, eye_gaze_threshold_sec = 3, created_by_admin_id = admins[2].id, locked_at = now.AddDays(-19).AddHours(-3), closed_at = now.AddDays(-19).AddMinutes(75), created_at = now.AddDays(-25) },
                new ExamSession { title = "CS201 Spring 2024",       course_tag = "CS201",   status = ExamSessionStatus.ARCHIVED,  start_time = now.AddMonths(-13), duration_minutes = 90,  question_bank_id = banks[2].id, grace_period_minutes = 10, login_window_minutes = 15, eye_gaze_threshold_sec = 3, created_by_admin_id = admins[3].id, locked_at = now.AddMonths(-13).AddHours(-3), closed_at = now.AddMonths(-13).AddMinutes(100), created_at = now.AddMonths(-14) },
            };

            await _context.ExamSessions.AddRangeAsync(sessions);
            await _context.SaveChangesAsync();
            return sessions;
        }

        private async Task SeedProctorSessionsAsync(List<ExamSession> sessions, List<User> proctors, DateTime now)
        {
            if (proctors.Count < 2) return;

            var proctorSessions = new List<ProctorSession>
            {
                // Proctor 1 (Farah) assigned to sessions 1, 2, 3
                new ProctorSession { exam_session_id = sessions[0].id, proctor_id = proctors[0].id, created_at = now, created_by = 1 },
                new ProctorSession { exam_session_id = sessions[1].id, proctor_id = proctors[0].id, created_at = now, created_by = 1 },
                new ProctorSession { exam_session_id = sessions[2].id, proctor_id = proctors[0].id, created_at = now, created_by = 1 },

                // Proctor 2 (Amira) assigned to sessions 3, 4, 5
                new ProctorSession { exam_session_id = sessions[2].id, proctor_id = proctors[1].id, created_at = now, created_by = 1 },
                new ProctorSession { exam_session_id = sessions[3].id, proctor_id = proctors[1].id, created_at = now, created_by = 1 },
                new ProctorSession { exam_session_id = sessions[4].id, proctor_id = proctors[1].id, created_at = now, created_by = 1 },
            };

            await _context.ProctorSessions.AddRangeAsync(proctorSessions);
            await _context.SaveChangesAsync();
        }

        private async Task<List<StudentSession>> SeedStudentSessionsAsync(List<ExamSession> sessions, List<Student> students, DateTime now)
        {
            var activeSession = sessions.First(s => s.status == ExamSessionStatus.ACTIVE);
            var graceSession = sessions.First(s => s.status == ExamSessionStatus.GRACE);
            var lockedSession = sessions.First(s => s.status == ExamSessionStatus.LOCKED);
            var closedSession = sessions.First(s => s.status == ExamSessionStatus.CLOSED);

            var studentSessions = new List<StudentSession>
            {
                // Active exam: students currently in the exam
                new StudentSession { exam_session_id = activeSession.id, student_id = students[0].id, status = StudentSessionStatus.InExam, login_at = now.AddMinutes(-38), verified_at = now.AddMinutes(-37), liveness_passed = true, face_match_passed = true, created_at = now.AddMinutes(-38) },
                new StudentSession { exam_session_id = activeSession.id, student_id = students[1].id, status = StudentSessionStatus.InExam, login_at = now.AddMinutes(-36), verified_at = now.AddMinutes(-35), liveness_passed = true, face_match_passed = true, created_at = now.AddMinutes(-36) },
                new StudentSession { exam_session_id = activeSession.id, student_id = students[2].id, status = StudentSessionStatus.InExam, login_at = now.AddMinutes(-33), verified_at = now.AddMinutes(-32), liveness_passed = true, face_match_passed = true, created_at = now.AddMinutes(-33) },
                new StudentSession { exam_session_id = activeSession.id, student_id = students[3].id, status = StudentSessionStatus.Disconnected, login_at = now.AddMinutes(-30), verified_at = now.AddMinutes(-29), liveness_passed = true, face_match_passed = true, created_at = now.AddMinutes(-30) },

                // Grace period session
                new StudentSession { exam_session_id = graceSession.id, student_id = students[4].id, status = StudentSessionStatus.InExam,    login_at = now.AddMinutes(-68), verified_at = now.AddMinutes(-67), liveness_passed = true, face_match_passed = true, created_at = now.AddMinutes(-68) },
                new StudentSession { exam_session_id = graceSession.id, student_id = students[5].id, status = StudentSessionStatus.Submitted, login_at = now.AddMinutes(-69), verified_at = now.AddMinutes(-68), liveness_passed = true, face_match_passed = true, submitted_at = now.AddMinutes(-5), awarded_marks = 5, created_at = now.AddMinutes(-69) },

                // Locked (upcoming) session: roster assigned, nobody started yet
                new StudentSession { exam_session_id = lockedSession.id, student_id = students[6].id, status = StudentSessionStatus.NotStarted, created_at = now.AddDays(-1) },
                new StudentSession { exam_session_id = lockedSession.id, student_id = students[7].id, status = StudentSessionStatus.NotStarted, created_at = now.AddDays(-1) },

                // Closed session: submitted with marks
                new StudentSession { exam_session_id = closedSession.id, student_id = students[0].id, status = StudentSessionStatus.Submitted, login_at = closedSession.start_time.AddMinutes(2), verified_at = closedSession.start_time.AddMinutes(3), liveness_passed = true, face_match_passed = true, submitted_at = closedSession.start_time.AddMinutes(55), awarded_marks = 3, created_at = closedSession.start_time },
                new StudentSession { exam_session_id = closedSession.id, student_id = students[4].id, status = StudentSessionStatus.Submitted, login_at = closedSession.start_time.AddMinutes(1), verified_at = closedSession.start_time.AddMinutes(2), liveness_passed = true, face_match_passed = true, submitted_at = closedSession.start_time.AddMinutes(58), awarded_marks = 2, created_at = closedSession.start_time },
                new StudentSession { exam_session_id = closedSession.id, student_id = students[7].id, status = StudentSessionStatus.Terminated, login_at = closedSession.start_time.AddMinutes(5), verified_at = closedSession.start_time.AddMinutes(6), liveness_passed = true, face_match_passed = true, created_at = closedSession.start_time },
            };

            await _context.StudentSessions.AddRangeAsync(studentSessions);
            await _context.SaveChangesAsync();
            return studentSessions;
        }

        private async Task SeedMonitoringAndAlertsAsync(List<StudentSession> studentSessions, List<User> admins, DateTime now)
        {
            var layla = studentSessions[0];
            var karim = studentSessions[1];
            var omar = studentSessions[2];

            var monitoringEvents = new List<MonitoringEvent>
            {
                new MonitoringEvent { student_session_id = layla.id, event_type = "MultipleFaces",    event_details = "Two faces detected in camera frame", occured_at = now.AddMinutes(-12), created_at = now.AddMinutes(-12) },
                new MonitoringEvent { student_session_id = layla.id, event_type = "AppSwitch",        event_details = "Student switched away from the exam window", occured_at = now.AddMinutes(-8), created_at = now.AddMinutes(-8) },
                new MonitoringEvent { student_session_id = karim.id, event_type = "ConnectivityLost", event_details = "Connection dropped for 90 seconds", occured_at = now.AddMinutes(-6), created_at = now.AddMinutes(-6) },
                new MonitoringEvent { student_session_id = omar.id,  event_type = "GazeDeviation",    event_details = "Gaze away from screen for more than 3 seconds", occured_at = now.AddMinutes(-2), created_at = now.AddMinutes(-2) },
            };
            await _context.MonitoringEvents.AddRangeAsync(monitoringEvents);
            await _context.SaveChangesAsync();

            var alerts = new List<AlertEvent>
            {
                new AlertEvent { student_session_id = layla.id, monitoring_event_id = monitoringEvents[0].id, alert_type = "MultipleFaces",    triggered_at = monitoringEvents[0].occured_at, delivered_at = monitoringEvents[0].occured_at.AddSeconds(2), created_at = monitoringEvents[0].occured_at },
                new AlertEvent { student_session_id = layla.id, monitoring_event_id = monitoringEvents[1].id, alert_type = "AppSwitch",        triggered_at = monitoringEvents[1].occured_at, delivered_at = monitoringEvents[1].occured_at.AddSeconds(2), created_at = monitoringEvents[1].occured_at },
                new AlertEvent { student_session_id = karim.id, monitoring_event_id = monitoringEvents[2].id, alert_type = "ConnectivityLost", triggered_at = monitoringEvents[2].occured_at, delivered_at = monitoringEvents[2].occured_at.AddSeconds(3), created_at = monitoringEvents[2].occured_at },
                new AlertEvent { student_session_id = omar.id,  monitoring_event_id = monitoringEvents[3].id, alert_type = "GazeDeviation",    triggered_at = monitoringEvents[3].occured_at, delivered_at = null, created_at = monitoringEvents[3].occured_at },
            };
            await _context.AlertEvents.AddRangeAsync(alerts);
            await _context.SaveChangesAsync();

            // Handle the first alert (warn) and dismiss the third; the rest stay open
            var warnAction = new ProctorAction
            {
                alert_event_id = alerts[0].id,
                admin_id = admins[0].id,
                action_type = ProctorActionType.Warn,
                action_note = "Warned the student about a second person in the frame",
                acted_at = now.AddMinutes(-10),
                created_at = now.AddMinutes(-10)
            };
            var dismissAction = new ProctorAction
            {
                alert_event_id = alerts[2].id,
                admin_id = admins[1].id,
                action_type = ProctorActionType.Dismiss,
                action_note = "Known network issue at the exam hall, no violation",
                acted_at = now.AddMinutes(-4),
                created_at = now.AddMinutes(-4)
            };
            await _context.ProctorActions.AddRangeAsync(warnAction, dismissAction);
            await _context.SaveChangesAsync();

            var warning = new WarningMessage
            {
                proctor_action_id = warnAction.id,
                student_session_id = layla.id,
                message_text = "Please make sure you are alone in the room. This incident has been recorded.",
                sent_at = now.AddMinutes(-10),
                acknowledged_at = now.AddMinutes(-9),
                created_at = now.AddMinutes(-10)
            };
            await _context.WarningMessages.AddAsync(warning);
            await _context.SaveChangesAsync();
        }

        private async Task SeedAuditLogsAsync(List<ExamSession> sessions, List<User> admins, DateTime now)
        {
            var activeSession = sessions.First(s => s.status == ExamSessionStatus.ACTIVE);
            var lockedSession = sessions.First(s => s.status == ExamSessionStatus.LOCKED);
            var closedSession = sessions.First(s => s.status == ExamSessionStatus.CLOSED);

            var logs = new List<AuditLog>
            {
                new AuditLog { exam_session_id = activeSession.id, actor_id = admins[0].id, actor_type = "Admin", action = "SessionStarted",  entity_id = activeSession.id, entity_type = "ExamSession", details = "Exam session moved to ACTIVE",  occurred_at = activeSession.start_time, created_at = activeSession.start_time },
                new AuditLog { exam_session_id = lockedSession.id, actor_id = admins[2].id, actor_type = "Admin", action = "SessionLocked",   entity_id = lockedSession.id, entity_type = "ExamSession", details = "Session locked before start",   occurred_at = now.AddHours(-2), created_at = now.AddHours(-2) },
                new AuditLog { exam_session_id = closedSession.id, actor_id = admins[2].id, actor_type = "Admin", action = "SessionClosed",   entity_id = closedSession.id, entity_type = "ExamSession", details = "Session closed after grace period", occurred_at = closedSession.closed_at ?? now, created_at = closedSession.closed_at ?? now },
                new AuditLog { exam_session_id = activeSession.id, actor_id = admins[0].id, actor_type = "Admin", action = "WarningSent",     entity_id = activeSession.id, entity_type = "WarningMessage", details = "Warning sent to Layla Mansour (MultipleFaces)", occurred_at = now.AddMinutes(-10), created_at = now.AddMinutes(-10) },
            };

            await _context.AuditLogs.AddRangeAsync(logs);
            await _context.SaveChangesAsync();
        }
    }
}
