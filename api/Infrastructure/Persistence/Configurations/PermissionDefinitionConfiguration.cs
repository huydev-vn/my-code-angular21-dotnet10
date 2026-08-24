using Domain.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

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
            .HasMaxLength(128);

        builder.Property(permission => permission.Action)
            .HasMaxLength(128);

        builder.Property(permission => permission.Version)
            .IsConcurrencyToken()
            .HasDefaultValue(1);
    }
}
