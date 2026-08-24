using Domain.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal sealed class GroupPermissionConfiguration : IEntityTypeConfiguration<GroupPermission>
{
    public void Configure(EntityTypeBuilder<GroupPermission> builder)
    {
        builder.ToTable(
            "GroupPermissions",
            table => table.HasComment(
                "Bảng nối nhóm ↔ quyền chức năng: group được phép làm gì."));
        builder.HasKey(assignment => new { assignment.GroupId, assignment.PermissionId });

        builder.HasOne<UserGroup>()
            .WithMany()
            .HasForeignKey(assignment => assignment.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<PermissionDefinition>()
            .WithMany()
            .HasForeignKey(assignment => assignment.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
