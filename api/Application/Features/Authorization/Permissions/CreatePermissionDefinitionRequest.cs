namespace Application.Features.Authorization.Permissions;

public sealed record CreatePermissionDefinitionRequest(
    string Code,
    string Name,
    string? Module,
    string? Action);
