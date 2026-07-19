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
     public class RefreshTokenConfiguration :IEntityTypeConfiguration <RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("RefreshToken");

            builder.HasKey(rt => rt.id);

            builder.Property(rt => rt.token)
                   .IsRequired()
                   .HasMaxLength(500);

            builder.Property(rt => rt.expires_at)
                   .IsRequired();

            builder.Property(rt => rt.replaced_by_token)
                   .HasMaxLength(500);

            builder.HasIndex(rt => rt.token).IsUnique();

            builder.HasOne(rt => rt.User)
                   .WithMany(u => u.RefreshTokens)
                   .HasForeignKey(rt => rt.user_id)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
