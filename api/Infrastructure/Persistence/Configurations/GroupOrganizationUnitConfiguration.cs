using Domain.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal sealed class GroupOrganizationUnitConfiguration
    : IEntityTypeConfiguration<GroupOrganizationUnit>
{
    public void Configure(EntityTypeBuilder<GroupOrganizationUnit> builder)
    {
        builder.ToTable(
            "GroupOrganizationUnits",
            table => table.HasComment(
                "Bảng nối nhóm ↔ đơn vị: phạm vi dữ liệu của group (đơn vị và các con)."));
        builder.HasKey(assignment => new { assignment.GroupId, assignment.OrganizationUnitId });

        builder.HasOne<UserGroup>()
            .WithMany()
            .HasForeignKey(assignment => assignment.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<OrganizationUnit>()
            .WithMany()
            .HasForeignKey(assignment => assignment.OrganizationUnitId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
