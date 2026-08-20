using Domain.Authorization;

namespace Domain.Tests;

public sealed class OrganizationUnitHierarchyTests
{
    [Fact]
    public void CollectAccessibleIds_includes_root_and_descendants()
    {
        var root = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var child = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var grandchild = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var siblingTree = Guid.Parse("44444444-4444-4444-4444-444444444444");

        var units = new (Guid Id, Guid? ParentId)[]
        {
            (root, null),
            (child, root),
            (grandchild, child),
            (siblingTree, null)
        };

        var accessible = OrganizationUnitHierarchy.CollectAccessibleIds([root], units);

        Assert.Equal(new[] { root, child, grandchild }, accessible.ToArray());
    }

    [Fact]
    public void CollectAccessibleIds_skips_unknown_roots_and_breaks_cycles()
    {
        var a = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var b = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var unknown = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        var units = new (Guid Id, Guid? ParentId)[]
        {
            (a, b),
            (b, a)
        };

        var accessible = OrganizationUnitHierarchy.CollectAccessibleIds([a, unknown], units);

        Assert.Equal(2, accessible.Count);
        Assert.Contains(a, accessible);
        Assert.Contains(b, accessible);
    }

    [Fact]
    public void WouldCreateCycle_detects_self_and_ancestor_loops()
    {
        var root = Guid.NewGuid();
        var child = Guid.NewGuid();
        var parentById = new Dictionary<Guid, Guid?>
        {
            [root] = null,
            [child] = root
        };

        Assert.False(OrganizationUnitHierarchy.WouldCreateCycle(child, null, parentById));
        Assert.True(OrganizationUnitHierarchy.WouldCreateCycle(child, child, parentById));
        Assert.True(OrganizationUnitHierarchy.WouldCreateCycle(root, child, parentById));
        Assert.False(OrganizationUnitHierarchy.WouldCreateCycle(child, root, parentById));
    }
}
