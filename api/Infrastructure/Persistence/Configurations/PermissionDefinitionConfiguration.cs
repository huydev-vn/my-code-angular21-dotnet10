using Domain.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Persistence.Configurations;

internal sealed class PermissionDefinitionConfiguration
    : IEntityTypeConfiguration<PermissionDefinition>
{
    public void Configure(EntityTypeBuilder<PermissionDefinition> builder)
    {
        builder.ToTable(
            "PermissionDefinitions",
            table => table.HasComment(
                "Danh mục quyền chức năng (permission catalog), ví dụ users.read. Không phải nhóm user."));
        builder.HasKey(permission => permission.Id);

        builder.Property(permission => permission.Code)
            .HasMaxLength(128)
            .IsRequired()
            .HasComment("Mã quyền ổn định dùng trong policy.");

        builder.HasIndex(permission => permission.Code)
            .IsUnique();

        builder.Property(permission => permission.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(permission => permission.Module)
            .HasMaxLength(128)
            .HasComment("Display/grouping module; Resource is the stable enforcement key.");

        builder.Property(permission => permission.Action)
            .HasMaxLength(128);

        builder.Property(permission => permission.Resource)
            .HasMaxLength(128)
            .HasComment("Stable resource key for future scope enforcement (e.g. users, authorization.permissions).");

        builder.Property(permission => permission.ScopeMode)
            .HasMaxLength(32)
            .HasConversion(new EnumToStringConverter<PermissionScopeMode>())
            .IsRequired()
            .HasDefaultValue(PermissionScopeMode.Global)
            .HasComment("None | OrganizationUnit | Global — catalog metadata for scope enforcement.");

        builder.Property(permission => permission.RiskLevel)
            .HasMaxLength(32)
            .HasConversion(new EnumToStringConverter<PermissionRiskLevel>())
            .IsRequired()
            .HasDefaultValue(PermissionRiskLevel.Medium)
            .HasComment("Low | Medium | High | Critical — Critical is privileged-assignable-only.");

        builder.Property(permission => permission.IsSystemManaged)
            .IsRequired()
            .HasDefaultValue(false)
            .HasComment("True for seeded system permissions; Code remains immutable either way.");

        builder.Property(permission => permission.Version)
            .IsConcurrencyToken()
            .HasDefaultValue(1);
    }
}
