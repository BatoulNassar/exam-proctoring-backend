using ExamProctoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamProctoring.Infrastructure.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("User");
            builder.HasKey(u => u.id);
            builder.Property(u => u.user_name).IsRequired().HasMaxLength(50);
            builder.Property(u => u.email).IsRequired().HasMaxLength(100);
            builder.Property(u => u.phone_number).IsRequired().HasMaxLength(20);
            builder.Property(u => u.full_name).IsRequired().HasMaxLength(100);

            builder.HasIndex(u => u.user_name).IsUnique();
            builder.HasIndex(u => u.email).IsUnique();
            builder.ConfigureAuditFields();
        }
    }
}
