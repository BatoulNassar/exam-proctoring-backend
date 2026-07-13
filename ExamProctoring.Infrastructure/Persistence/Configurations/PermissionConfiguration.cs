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
    public class PermissionConfiguration :IEntityTypeConfiguration <Permission>
    {
        public void Configure(EntityTypeBuilder<Permission> builder) {
            builder.ToTable("Permission");
            builder.HasKey(p => p.id);
            builder.Property(p => p.name).IsRequired().HasMaxLength(100);
            builder.Property(p => p.description).IsRequired().HasMaxLength(255);

            builder.HasIndex(p => p.name).IsUnique();
            builder.ConfigureAuditFields();
        }

    }
}
