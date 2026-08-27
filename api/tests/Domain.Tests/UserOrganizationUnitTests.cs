using Domain.Authorization;

namespace Domain.Tests;

public sealed class UserOrganizationUnitTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 27, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_SetsPrimaryActiveMembership()
    {
        var userId = Guid.NewGuid();
        var ouId = Guid.NewGuid();

        var membership = UserOrganizationUnit.Create(
            userId,
            ouId,
            OrganizationUnitRelationship.Primary,
            Now);

        Assert.Equal(userId, membership.UserId);
        Assert.Equal(ouId, membership.OrganizationUnitId);
        Assert.Equal(OrganizationUnitRelationship.Primary, membership.Relationship);
        Assert.True(membership.IsActive);
        Assert.Equal(Now, membership.AssignedAt);
    }

    [Fact]
    public void Deactivate_ClearsActiveFlag()
    {
        var membership = UserOrganizationUnit.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            OrganizationUnitRelationship.Additional,
            Now);

        membership.Deactivate();

        Assert.False(membership.IsActive);
    }

    [Fact]
    public void Reactivate_RestoresActiveAndRelationship()
    {
        var membership = UserOrganizationUnit.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            OrganizationUnitRelationship.Additional,
            Now);
        membership.Deactivate();

        var later = Now.AddHours(1);
        membership.Reactivate(OrganizationUnitRelationship.Primary, later);

        Assert.True(membership.IsActive);
        Assert.Equal(OrganizationUnitRelationship.Primary, membership.Relationship);
        Assert.Equal(later, membership.AssignedAt);
    }

    [Fact]
    public void WouldViolateSinglePrimary_WhenAnotherActivePrimaryExists()
    {
        var userId = Guid.NewGuid();
        var primaryOu = Guid.NewGuid();
        var otherOu = Guid.NewGuid();
        var existing = UserOrganizationUnit.Create(
            userId,
            primaryOu,
            OrganizationUnitRelationship.Primary,
            Now);

        Assert.True(
            UserOrganizationUnit.WouldViolateSinglePrimary(
                [existing],
                otherOu,
                OrganizationUnitRelationship.Primary));
    }

    [Fact]
    public void WouldViolateSinglePrimary_False_WhenSameOuOrAdditional()
    {
        var userId = Guid.NewGuid();
        var primaryOu = Guid.NewGuid();
        var existing = UserOrganizationUnit.Create(
            userId,
            primaryOu,
            OrganizationUnitRelationship.Primary,
            Now);

        Assert.False(
            UserOrganizationUnit.WouldViolateSinglePrimary(
                [existing],
                primaryOu,
                OrganizationUnitRelationship.Primary));
        Assert.False(
            UserOrganizationUnit.WouldViolateSinglePrimary(
                [existing],
                Guid.NewGuid(),
                OrganizationUnitRelationship.Additional));
    }
}
