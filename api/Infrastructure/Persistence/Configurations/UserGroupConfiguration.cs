using Domain.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal sealed class UserGroupConfiguration : IEntityTypeConfiguration<UserGroup>
{
    public void Configure(EntityTypeBuilder<UserGroup> builder)
    {
        builder.ToTable("UserGroups");
        builder.HasKey(group => group.Id);

        builder.Property(group => group.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(group => group.Name)
            .IsUnique();

        builder.Property(group => group.Description)
            .HasMaxLength(1024);
    }
}
