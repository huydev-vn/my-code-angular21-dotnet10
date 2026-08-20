using Application.Common.Persistence;

namespace Infrastructure.Persistence;

internal sealed class EfUnitOfWork(AppDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        DbUpdateConflict.SaveChangesAsync(dbContext, cancellationToken);
}
