using Domain.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal sealed class OrganizationUnitConfiguration : IEntityTypeConfiguration<OrganizationUnit>
{
    public void Configure(EntityTypeBuilder<OrganizationUnit> builder)
    {
        builder.ToTable(
            "OrganizationUnits",
            table => table.HasComment(
                "Cây đơn vị tổ chức/phòng ban — phạm vi dữ liệu cha-con."));
        builder.HasKey(unit => unit.Id);

        builder.Property(unit => unit.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(unit => unit.Code)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(unit => unit.Code)
            .IsUnique();

        builder.HasOne<OrganizationUnit>()
            .WithMany()
            .HasForeignKey(unit => unit.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(unit => unit.Version)
            .IsConcurrencyToken()
            .HasDefaultValue(1);
    }
}
