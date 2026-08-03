using ExamProctoring.Application.Common;
using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Application.Common.Settings;
using ExamProctoring.Application.Features.Alerts.Services;
using ExamProctoring.Application.Features.AuditLogs.Services;
using ExamProctoring.Application.Features.Auth.Services;
using ExamProctoring.Application.Features.Auth.Validators;
using ExamProctoring.Application.Features.Dashboard.Services;
using ExamProctoring.Application.Features.ExamSessions.Services;
using ExamProctoring.Application.Features.QuestionBank.Services;
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

// ===== Repositories =====
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IPermissionRepository, PermissionRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IAlertRepository, AlertRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IUserRoleRepository, UserRoleRepository>();
builder.Services.AddScoped<IExamSessionRepository, ExamSessionRepository>();
builder.Services.AddScoped<IProctorSessionRepository, ProctorSessionRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<IStudentLoginAttemptRepository, StudentLoginAttemptRepository>();
builder.Services.AddScoped<IPermissionRoleRepository, PermissionRoleRepository>();
builder.Services.AddScoped<IQuestionBankRepository, QuestionBankRepository>();

// ===== Infrastructure services =====
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

// ===== Application services =====
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IExamSessionService, ExamSessionService>();
builder.Services.AddScoped<IExamSessionStateTransitionService, ExamSessionStateTransitionService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IQuestionBankService, QuestionBankService>();
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();

// ===== Student desktop (Flutter Windows) services =====
builder.Services.AddScoped<IStudentAuthService, StudentAuthService>();

// Background services
builder.Services.AddHostedService<ExamSessionStateCheckBackgroundService>();

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
});

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

app.Run();