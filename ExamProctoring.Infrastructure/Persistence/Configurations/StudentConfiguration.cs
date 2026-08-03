using ExamProctoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamProctoring.Infrastructure.Persistence.Configurations
{
    public class StudentConfiguration : IEntityTypeConfiguration<Student>
    {
        public void Configure(EntityTypeBuilder<Student> builder)
        {
            builder.ToTable("Student");
            builder.HasKey(s => s.id);
            builder.Property(s => s.user_name).IsRequired().HasMaxLength(50);
            builder.Property(s => s.password).IsRequired().HasMaxLength(255);
            builder.Property(s => s.email).IsRequired().HasMaxLength(100);
            builder.Property(s => s.phone_number).IsRequired().HasMaxLength(20);
            builder.Property(s=>s.first_name).IsRequired().HasMaxLength(50);
            builder.Property(s => s.middle_name).IsRequired(false).HasMaxLength(50);
            builder.Property(s=>s.last_name).IsRequired().HasMaxLength(50);
            builder.Property(s=>s.university_number).IsRequired().HasMaxLength(20);
            builder.Property(s=>s.face_id).IsRequired().HasMaxLength(500);

            // Student desktop authentication state. Defaults keep existing and newly
            // imported students active with a clean counter.
            builder.Property(s => s.is_active).IsRequired().HasDefaultValue(true);
            builder.Property(s => s.failed_login_attempts).IsRequired().HasDefaultValue(0);
            builder.Property(s => s.lockout_end_utc).IsRequired(false);


            builder.HasIndex(s => s.user_name).IsUnique();
            builder.HasIndex(s => s.email).IsUnique();
            builder.HasIndex(s => s.university_number).IsUnique();
            builder.ConfigureAuditFields();
        }
    }
}
