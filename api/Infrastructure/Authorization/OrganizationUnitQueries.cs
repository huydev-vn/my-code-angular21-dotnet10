using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Authorization;

/// <summary>
/// Database-backed organization-unit hierarchy helpers that expand only the
/// requested subtree instead of loading the full table.
/// </summary>
internal static class OrganizationUnitQueries
{
    public static async Task<IReadOnlyList<Guid>> CollectAccessibleIdsAsync(
        AppDbContext dbContext,
        IReadOnlyCollection<Guid> rootIds,
        bool activeOnly,
        CancellationToken cancellationToken)
    {
        if (rootIds.Count == 0)
        {
            return [];
        }

        var roots = rootIds.Distinct().ToArray();
        var accessible = new HashSet<Guid>();
        var frontier = new List<Guid>();

        var existingRootsQuery = dbContext.OrganizationUnits
            .AsNoTracking()
            .Where(unit => roots.Contains(unit.Id));

        if (activeOnly)
        {
            existingRootsQuery = existingRootsQuery.Where(unit => unit.IsActive);
        }

        var existingRoots = await existingRootsQuery
            .Select(unit => unit.Id)
            .ToListAsync(cancellationToken);

        foreach (var rootId in existingRoots)
        {
            if (accessible.Add(rootId))
            {
                frontier.Add(rootId);
            }
        }

        while (frontier.Count > 0)
        {
            var parentIds = frontier.ToArray();
            frontier.Clear();

            var childrenQuery = dbContext.OrganizationUnits
                .AsNoTracking()
                .Where(unit =>
                    unit.ParentId != null &&
                    parentIds.Contains(unit.ParentId.Value));

            if (activeOnly)
            {
                childrenQuery = childrenQuery.Where(unit => unit.IsActive);
            }

            var children = await childrenQuery
                .Select(unit => unit.Id)
                .ToListAsync(cancellationToken);

            foreach (var childId in children)
            {
                if (accessible.Add(childId))
                {
                    frontier.Add(childId);
                }
            }
        }

        return accessible.OrderBy(id => id).ToArray();
    }
}
