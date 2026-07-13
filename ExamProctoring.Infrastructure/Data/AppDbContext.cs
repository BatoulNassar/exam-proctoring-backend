using ExamProctoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamProctoring.Infrastructure.Data
{
     public class AppDbContext :DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<User_Roles> UserRoles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<PermissionRole> PermissionRoles { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<QuestionBank> QuestionBanks { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<ExamSession> ExamSessions { get; set; }
        public DbSet<StudentSession> StudentSessions { get; set; }
        public DbSet<StudentAnswer> StudentAnswers { get; set; }
        public DbSet<AutoScore> AutoScores { get; set; }
        public DbSet<ConnectivityBuffer> ConnectivityBuffers { get; set; }
        public DbSet<MonitoringEvent> MonitoringEvents { get; set; }
        public DbSet<AlertEvent> AlertEvents { get; set; }
        public DbSet<ProctorAction> ProctorActions { get; set; }
        public DbSet<WarningMessage> WarningMessages { get; set; }
        public DbSet<GradingExport> GradingExports { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(AppDbContext).Assembly);
        }
    }
}
