using Domain.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal sealed class UserGroupConfiguration : IEntityTypeConfiguration<UserGroup>
{
    public void Configure(EntityTypeBuilder<UserGroup> builder)
    {
        builder.ToTable(
            "UserGroups",
            table => table.HasComment(
                "Nhóm phân quyền nghiệp vụ: tập hợp permission chức năng và phạm vi đơn vị."));
        builder.HasKey(group => group.Id);

        builder.Property(group => group.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(group => group.Name)
            .IsUnique();

        builder.Property(group => group.Description)
            .HasMaxLength(1024);

        builder.Property(group => group.IsPrivileged)
            .IsRequired()
            .HasDefaultValue(false)
            .HasComment(
                "Privileged bootstrap groups (global admin). Membership and high-risk permissions are restricted.");

        builder.Property(group => group.Version)
            .IsConcurrencyToken()
            .HasDefaultValue(1);
    }
}
