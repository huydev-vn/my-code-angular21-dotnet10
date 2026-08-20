namespace Domain.Authorization;

/// <summary>
/// Pure hierarchy helpers for organization-unit trees. Keeps traversal out of
/// persistence so the same rules can be unit-tested without a database.
/// </summary>
public static class OrganizationUnitHierarchy
{
    public static IReadOnlyList<Guid> CollectAccessibleIds(
        IEnumerable<Guid> rootIds,
        IReadOnlyCollection<(Guid Id, Guid? ParentId)> units)
    {
        ArgumentNullException.ThrowIfNull(rootIds);
        ArgumentNullException.ThrowIfNull(units);

        var knownIds = units.Select(unit => unit.Id).ToHashSet();
        var childrenByParent = units
            .Where(unit => unit.ParentId is not null)
            .GroupBy(unit => unit.ParentId!.Value)
            .ToDictionary(
                group => group.Key,
                group => group.Select(unit => unit.Id).ToArray());

        var accessible = new HashSet<Guid>();
        foreach (var rootId in rootIds)
        {
            if (!knownIds.Contains(rootId))
            {
                continue;
            }

            CollectDescendants(rootId, childrenByParent, accessible);
        }

        return accessible.OrderBy(id => id).ToArray();
    }

    public static bool WouldCreateCycle(
        Guid organizationUnitId,
        Guid? newParentId,
        IReadOnlyDictionary<Guid, Guid?> parentById)
    {
        ArgumentNullException.ThrowIfNull(parentById);

        if (newParentId is null)
        {
            return false;
        }

        if (newParentId.Value == organizationUnitId)
        {
            return true;
        }

        var current = newParentId;
        var guard = 0;
        while (current is Guid parentId)
        {
            if (parentId == organizationUnitId)
            {
                return true;
            }

            if (!parentById.TryGetValue(parentId, out current))
            {
                break;
            }

            if (++guard > parentById.Count)
            {
                return true;
            }
        }

        return false;
    }

    private static void CollectDescendants(
        Guid rootId,
        IReadOnlyDictionary<Guid, Guid[]> childrenByParent,
        ISet<Guid> destination)
    {
        if (!destination.Add(rootId))
        {
            return;
        }

        if (!childrenByParent.TryGetValue(rootId, out var children))
        {
            return;
        }

        foreach (var childId in children)
        {
            CollectDescendants(childId, childrenByParent, destination);
        }
    }
}
