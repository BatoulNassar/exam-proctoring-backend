using ExamProctoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamProctoring.Infrastructure.Persistence.Configurations
{
    public class RoleConfiguration :IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.ToTable("Role");
            builder.HasKey(r => r.id);
            builder.Property(r=>r.name).IsRequired().HasMaxLength(50);

            builder.HasIndex(r => r.name).IsUnique();
            builder.ConfigureAuditFields();
        }
    }
}
