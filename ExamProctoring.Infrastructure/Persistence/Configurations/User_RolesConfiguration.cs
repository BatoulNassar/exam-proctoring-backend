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
    public class User_RolesConfiguration :IEntityTypeConfiguration <User_Roles>
    {
        public void Configure(EntityTypeBuilder<User_Roles> builder) {
            builder.ToTable("User_Roles");
            builder.HasKey(ur => ur.id );

            builder.HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.user_id);

            builder.HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.role_id);
            builder.ConfigureAuditFields();
        }
    }
}
