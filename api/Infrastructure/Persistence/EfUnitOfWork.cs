using Application.Common.Persistence;
using Application.Features.Authorization.Abstractions;
using Domain.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Persistence;

internal sealed class EfUnitOfWork(
    AppDbContext dbContext,
    IAuthorizationStateVersion authorizationStateVersion) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        var authorizationChanged = HasAuthorizationChanges();
        var saved = await DbUpdateConflict.SaveChangesAsync(dbContext, cancellationToken);
        if (authorizationChanged)
        {
            // Bump only after PostgreSQL commit so replicas never see a newer
            // cache version pointing at uncommitted authorization state.
            await authorizationStateVersion.BumpAsync(cancellationToken);
        }

        return saved;
    }

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken)
    {
        var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        return new EfUnitOfWorkTransaction(transaction);
    }

    private bool HasAuthorizationChanges() =>
        dbContext.ChangeTracker.Entries().Any(entry =>
            entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted &&
            entry.Entity is PermissionDefinition
                or UserGroup
                or OrganizationUnit
                or GroupPermission
                or UserGroupMembership
                or GroupOrganizationUnit);
    // Agent C: UserOrganizationUnit is intentionally omitted — it does not affect
    // permission/scope cache and must not bump the shared authorization state version.

    private sealed class EfUnitOfWorkTransaction(IDbContextTransaction transaction)
        : IUnitOfWorkTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken) =>
            transaction.CommitAsync(cancellationToken);

        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
