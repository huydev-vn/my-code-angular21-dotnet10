using Domain.Authorization;
using Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Persistence.Configurations;

internal sealed class UserOrganizationUnitConfiguration
    : IEntityTypeConfiguration<UserOrganizationUnit>
{
    public void Configure(EntityTypeBuilder<UserOrganizationUnit> builder)
    {
        builder.ToTable(
            "UserOrganizationUnits",
            table => table.HasComment(
                "User↔OU organizational membership (Primary/Additional). " +
                "Does NOT grant permissions or data access; group→OU scope does."));
        builder.HasKey(membership => new { membership.UserId, membership.OrganizationUnitId });

        builder.Property(membership => membership.Relationship)
            .HasMaxLength(32)
            .HasConversion(new EnumToStringConverter<OrganizationUnitRelationship>())
            .IsRequired()
            .HasComment("Primary | Additional — organizational affiliation only.");

        builder.Property(membership => membership.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(membership => membership.OrganizationUnitId);

        // At most one active Primary per user (enforced in app + DB).
        builder.HasIndex(membership => membership.UserId)
            .IsUnique()
            .HasFilter("\"IsActive\" = TRUE AND \"Relationship\" = 'Primary'")
            .HasDatabaseName("IX_UserOrganizationUnits_UserId_ActivePrimary");

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(membership => membership.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<OrganizationUnit>()
            .WithMany()
            .HasForeignKey(membership => membership.OrganizationUnitId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
