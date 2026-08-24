using Domain.Authorization;
using Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal sealed class UserGroupMembershipConfiguration
    : IEntityTypeConfiguration<UserGroupMembership>
{
    public void Configure(EntityTypeBuilder<UserGroupMembership> builder)
    {
        builder.ToTable(
            "UserGroupMemberships",
            table => table.HasComment(
                "Bảng nối user ↔ nhóm phân quyền: user thuộc group nào."));
        builder.HasKey(membership => new { membership.UserId, membership.GroupId });

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(membership => membership.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<UserGroup>()
            .WithMany()
            .HasForeignKey(membership => membership.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
