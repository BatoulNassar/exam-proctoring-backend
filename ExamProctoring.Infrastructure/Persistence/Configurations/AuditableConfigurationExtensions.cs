using ExamProctoring.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public static class AuditableConfigurationExtensions
{
    public static void ConfigureAuditFields<T>(this EntityTypeBuilder<T> builder)
        where T : BaseEntity
    {
        builder.Property(e => e.created_at).IsRequired();
        builder.Property(e => e.created_by);
        builder.Property(e => e.updated_at);
        builder.Property(e => e.updated_by);

        builder.Property(e => e.is_deleted).IsRequired().HasDefaultValue(false);

        builder.Property(e => e.deleted_at);
        builder.Property(e => e.deleted_by);
    }
}