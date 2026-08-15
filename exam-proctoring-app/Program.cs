using ExamProctoring.Application.Common;
using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Application.Common.Settings;
using ExamProctoring.Application.Features.Alerts.Services;
using ExamProctoring.Application.Features.Monitoring.Services;
using ExamProctoring.Application.Features.Settings.Services;
using ExamProctoring.API.Hubs;
using ExamProctoring.Application.Features.AuditLogs.Services;
using ExamProctoring.Application.Features.Auth.Services;
using ExamProctoring.Application.Features.Auth.Validators;
using ExamProctoring.Application.Features.Dashboard.Services;
using ExamProctoring.Application.Features.DeviceChecks.Services;
using ExamProctoring.Application.Features.Eligibility.Services;
using ExamProctoring.Application.Features.ExamAttempts.Services;
using ExamProctoring.Application.Features.ExamSessions.Services;
using ExamProctoring.Application.Features.IdentityVerification;
using ExamProctoring.Application.Features.IdentityVerification.Services;
using ExamProctoring.Application.Features.QuestionBank.Services;
using ExamProctoring.API.Services;
using ExamProctoring.Application.Features.Roles.Services;
using ExamProctoring.Application.Features.Students.Services;
using ExamProctoring.Application.Features.StudentAuth.Services;
using ExamProctoring.Application.Features.Users.Services;
using ExamProctoring.Application.Interfaces;
using ExamProctoring.API.Common;
using ExamProctoring.API.Extensions;
using ExamProctoring.API.Middleware;
using ExamProctoring.API.Services;
using ExamProctoring.Infrastructure.Data;
using ExamProctoring.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using ExamProctoring.Infrastructure.Persistence;
using ExamProctoring.Infrastructure.Persistence.Repositories;
using ExamProctoring.Infrastructure.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "exam-proctoring-app", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "أدخل التوكن فقط بدون كلمة Bearer، مثال: eyJhbGciOi..."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ServerConnection")));

// ���������
builder.Services.AddOptions<JwtSettings>()
    .Bind(builder.Configuration.GetSection("Jwt"))
    .Validate(s => s.StudentAccessTokenExpirationMinutes > 0,
        "Jwt:StudentAccessTokenExpirationMinutes must be a positive number of minutes.")
    .ValidateOnStart();

builder.Services.AddOptions<StudentAuthenticationSettings>()
    .Bind(builder.Configuration.GetSection("StudentAuthentication"))
    .Validate(s => s.MaxFailedLoginAttempts > 0,
        "StudentAuthentication:MaxFailedLoginAttempts must be greater than 0.")
    .Validate(s => s.LockoutDurationMinutes > 0,
        "StudentAuthentication:LockoutDurationMinutes must be greater than 0.")
    .ValidateOnStart();

builder.Services.AddOptions<StudentApplicationSettings>()
    .Bind(builder.Configuration.GetSection("StudentApplication"))
    .Validate(s => AppVersion.TryParse(s.MinimumSupportedVersion, out _),
        "StudentApplication:MinimumSupportedVersion is required and must look like 1.0.0 or 1.0.0+1.")
    .ValidateOnStart();

// Temporary home for the three monitoring thresholds that have no database column yet.
// See MonitoringPolicySettings; these move once the realtime monitoring feature lands.
builder.Services.AddOptions<MonitoringPolicySettings>()
    .Bind(builder.Configuration.GetSection("MonitoringPolicy"))
    .Validate(s => s.AudioNoiseThresholdDb < 0,
        "MonitoringPolicy:AudioNoiseThresholdDb must be negative, for example -25.")
    .Validate(s => s.HeartbeatIntervalSeconds > 0,
        "MonitoringPolicy:HeartbeatIntervalSeconds must be greater than 0.")
    .Validate(s => s.ConnectivityLostThresholdSeconds > 0,
        "MonitoringPolicy:ConnectivityLostThresholdSeconds must be greater than 0.")
    .ValidateOnStart();

// ===== Repositories =====
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IPermissionRepository, PermissionRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IAlertRepository, AlertRepository>();
builder.Services.AddScoped<IAlertEventRepository, AlertEventRepository>();
builder.Services.AddScoped<IProctorActionRepository, ProctorActionRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IUserRoleRepository, UserRoleRepository>();
builder.Services.AddScoped<IExamSessionRepository, ExamSessionRepository>();
builder.Services.AddScoped<IProctorSessionRepository, ProctorSessionRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<IStudentLoginAttemptRepository, StudentLoginAttemptRepository>();
builder.Services.AddScoped<IStudentSessionRepository, StudentSessionRepository>();
builder.Services.AddScoped<IDeviceCheckRepository, DeviceCheckRepository>();
builder.Services.AddScoped<IAttemptRepository, AttemptRepository>();
builder.Services.AddScoped<IStudentAnswerRepository, StudentAnswerRepository>();
builder.Services.AddScoped<IIdempotencyRepository, IdempotencyRepository>();
builder.Services.AddScoped<IIdentityVerificationRepository, IdentityVerificationRepository>();
builder.Services.AddScoped<IAttemptFinalisationRepository, AttemptFinalisationRepository>();
builder.Services.AddScoped<IPermissionRoleRepository, PermissionRoleRepository>();
builder.Services.AddScoped<IQuestionBankRepository, QuestionBankRepository>();
builder.Services.AddScoped<ISystemSettingsRepository, SystemSettingsRepository>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();

// ===== Infrastructure services =====
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddOptions<EmailSettings>()
    .Bind(builder.Configuration.GetSection("Email"));

// ===== Application services =====
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<IMonitoringService, MonitoringService>();
builder.Services.AddScoped<ISystemSettingsService, SystemSettingsService>();
builder.Services.AddScoped<IMonitoringNotifier, SignalRMonitoringNotifier>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IExamSessionService, ExamSessionService>();
builder.Services.AddScoped<IExamSessionStateTransitionService, ExamSessionStateTransitionService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IProctorDashboardService, ProctorDashboardService>();
builder.Services.AddScoped<IQuestionBankService, QuestionBankService>();
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();

// ===== Student desktop (Flutter Windows) services =====
builder.Services.AddScoped<IStudentAuthService, StudentAuthService>();
builder.Services.AddScoped<IEligibilityService, EligibilityService>();
builder.Services.AddScoped<IDeviceCheckService, DeviceCheckService>();
builder.Services.AddScoped<IExamAttemptService, ExamAttemptService>();
builder.Services.AddScoped<IIdentityVerificationService, IdentityVerificationService>();

// The single terminal-transition path shared by student submit, automatic expiry and proctor
// termination. Registered before AlertService so the dependency direction stays obvious.
builder.Services.AddScoped<IAttemptFinalisationService, AttemptFinalisationService>();
builder.Services.AddScoped<IAttemptExpiryService, AttemptExpiryService>();

// The identity gate, now backed by the real Identity Verification feature. The seam is
// unchanged: ExamAttemptService still only asks whether identity is settled for an attempt.
builder.Services.AddScoped<IIdentityGate, PersistedIdentityGate>();

// Pure cosine similarity over canonical L2-normalised SFace vectors. No inference happens
// here - both vectors already exist by the time they reach the matcher.
builder.Services.AddSingleton<IFaceMatcher, CosineFaceMatcher>();

// Backend reference-face enrolment: YuNet + SFace over the trusted administrative photo.
// Singleton because the two ONNX models are loaded once and reused; the implementation
// serialises access itself, since neither native object is thread-safe.
builder.Services.AddOptions<FaceRecognitionSettings>()
    .Bind(builder.Configuration.GetSection(FaceRecognitionSettings.SectionName))
    .Validate(s => !string.IsNullOrWhiteSpace(s.ModelDirectory),
        "FaceRecognition:ModelDirectory must not be empty.")
    .Validate(s => s.MaxImageDimension >= 112,
        "FaceRecognition:MaxImageDimension must be at least 112, the SFace aligned-chip size.")
    .ValidateOnStart();

builder.Services.AddSingleton<IReferenceFaceEmbeddingGenerator, OpenCvReferenceFaceEmbeddingGenerator>();

// Background services
builder.Services.AddScoped<IQuestionBankStateTransitionService, QuestionBankStateTransitionService>();
builder.Services.AddHostedService<ExamSessionStateCheckBackgroundService>();
builder.Services.AddHostedService<QuestionBankStateCheckBackgroundService>();

// Validation
builder.Services.AddValidatorsFromAssembly(typeof(LoginRequestValidator).Assembly);

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>();
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine("AUTH FAILED");
            Console.WriteLine(context.Exception);
            return Task.CompletedTask;
        },

        OnTokenValidated = context =>
        {
            Console.WriteLine("TOKEN VALID");
            return Task.CompletedTask;
        },

        OnChallenge = context =>
        {
            Console.WriteLine("CHALLENGE");
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    // Dashboard tokens carry no token_type claim, so their behaviour is unchanged.
    // Only student desktop tokens (token_type = student) are rejected.
    options.AddPolicy(AuthorizationPolicies.DashboardOnly, policy =>
        policy.RequireAuthenticatedUser()
              .RequireAssertion(context =>
                  !context.User.HasClaim(c => c.Type == "token_type" && c.Value == "student")));

    // Student desktop tokens only: dashboard tokens and student tokens whose student_id
    // claim is missing, malformed, zero or negative are rejected.
    options.AddPolicy(AuthorizationPolicies.StudentOnly, policy =>
        policy.RequireAuthenticatedUser()
              .RequireClaim("token_type", "student")
              .RequireAssertion(context =>
                  int.TryParse(context.User.FindFirst("student_id")?.Value, out var studentId)
                  && studentId > 0));
});
builder.Services.AddAuthorization();
builder.Services.AddSignalR();

var app = builder.Build();

// Development applies migrations and seeds demo data; other environments touch no database at startup.
await app.InitializeDatabaseAsync();

// First in the pipeline: logs any unhandled request exception in full, returns a generic 500.
app.UseMiddleware<ExceptionLoggingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<MonitoringHub>("/ws/monitoring");

// Startup diagnostic for reference-face enrolment.
//
// Deliberately a log line rather than a hard failure: a missing model must not take the whole
// API down, because every other endpoint still works without it. But it must be visible at
// boot rather than discovered by an administrator halfway through importing a cohort - a
// wrong relative path or a model that did not reach the publish output is exactly the class
// of deployment problem this project has already hit once.
using (var startupScope = app.Services.CreateScope())
{
    var faceLogger = startupScope.ServiceProvider
        .GetRequiredService<ILoggerFactory>().CreateLogger("FaceRecognition.Startup");

    var generator = startupScope.ServiceProvider
        .GetRequiredService<IReferenceFaceEmbeddingGenerator>() as OpenCvReferenceFaceEmbeddingGenerator;

    if (generator == null)
    {
        faceLogger.LogWarning("Reference face enrolment is not backed by the OpenCV generator.");
    }
    else if (generator.ModelFilesExist)
    {
        faceLogger.LogInformation(
            "Face recognition models found. detector={Detector} recognizer={Recognizer}",
            generator.DetectorModelPath, generator.RecognizerModelPath);
    }
    else
    {
        faceLogger.LogError(
            "Face recognition model files are MISSING. Student import will fail identity enrolment. " +
            "Expected detector={Detector} recognizer={Recognizer}",
            generator.DetectorModelPath, generator.RecognizerModelPath);
    }
}

app.Run();