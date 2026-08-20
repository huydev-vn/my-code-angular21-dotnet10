using Application.Common.Errors;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Persistence;

internal static class DbUpdateConflict
{
    public static async Task<int> SaveChangesAsync(
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        try
        {
            return await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new PersistenceConflictException(
                "The resource was modified by another request.",
                exception);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            throw new PersistenceConflictException(
                "The change conflicted with existing data.",
                exception);
        }
    }

    public static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException postgres &&
        postgres.SqlState == PostgresErrorCodes.UniqueViolation;
}
