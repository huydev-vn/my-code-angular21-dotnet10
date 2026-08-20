namespace Application.Common.Persistence;

/// <summary>
/// Single atomic commit for the current request scope. Feature stores share
/// the same EF Core context; use cases should persist pending changes here.
/// Do not introduce a generic repository.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Starts a database transaction for the current request scope.
    /// Dispose without committing to roll back.
    /// </summary>
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
}

/// <summary>Ambient unit-of-work transaction for multi-step use cases.</summary>
public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
}
