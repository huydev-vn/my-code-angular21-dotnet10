using Application.Features.Authorization.Abstractions;

namespace Application.Features.Authorization.Abstractions;

public interface IAuthorizationDecisionService
{
    Task<UserAuthorizationContext?> GetContextAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<AuthorizationDecision> HasPermissionAsync(
        Guid userId,
        string permissionCode,
        CancellationToken cancellationToken);

    Task<AuthorizationDecision> HasPermissionOnUnitAsync(
        Guid userId,
        string permissionCode,
        Guid organizationUnitId,
        CancellationToken cancellationToken);

    Task<bool> CanAccessOrganizationUnitAsync(
        Guid userId,
        Guid organizationUnitId,
        CancellationToken cancellationToken);
}
