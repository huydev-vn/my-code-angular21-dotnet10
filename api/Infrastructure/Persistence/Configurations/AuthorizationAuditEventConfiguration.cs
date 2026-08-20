using Domain.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal sealed class AuthorizationAuditEventConfiguration
    : IEntityTypeConfiguration<AuthorizationAuditEvent>
{
    public void Configure(EntityTypeBuilder<AuthorizationAuditEvent> builder)
    {
        builder.ToTable("AuthorizationAuditEvents");
        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Action)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(entry => entry.EntityType)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(entry => entry.Data)
            .HasMaxLength(2048);

        builder.Property(entry => entry.TraceId)
            .HasMaxLength(128);

        builder.HasIndex(entry => entry.OccurredAt);
        builder.HasIndex(entry => new { entry.EntityType, entry.EntityId });
        builder.HasIndex(entry => entry.ActorUserId);
    }
}
