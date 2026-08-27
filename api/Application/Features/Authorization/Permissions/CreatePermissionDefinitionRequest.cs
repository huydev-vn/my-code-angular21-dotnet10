using Domain.Authorization;

namespace Application.Features.Authorization.Permissions;

/// <summary>Payload for creating a permission catalog entry.</summary>
public sealed record CreatePermissionDefinitionRequest(
    string Code,
    string Name,
    string? Module,
    string? Action,
    string? Resource,
    PermissionScopeMode ScopeMode,
    PermissionRiskLevel RiskLevel = PermissionRiskLevel.Medium);
