using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ExamProctoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExamProctoring.Infrastructure.Persistence.Configurations
{
    public class Permission_RoleConfiguration :IEntityTypeConfiguration <PermissionRole>
    {
        public void Configure(EntityTypeBuilder <PermissionRole> builder) {
            builder.ToTable("Permission_Role");
            builder.HasKey(pr => pr.id );

            builder.HasOne(pr => pr.Role)
                .WithMany(r => r.PermissionRoles)
                .HasForeignKey(pr => pr.role_id)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(pr => pr.Permission)
                .WithMany(p => p.Permission_Roles)
                .HasForeignKey(pr => pr.permission_id)
                .OnDelete(DeleteBehavior.Cascade);
            builder.ConfigureAuditFields();
        }
    }
}
