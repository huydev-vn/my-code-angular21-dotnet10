using Application.Features.Authorization.Assignments;
using Application.Features.Authorization.Audit;
using Application.Features.Authorization.GetContext;
using Application.Features.Authorization.Groups;
using Application.Features.Authorization.OrganizationUnits;
using Application.Features.Authorization.Permissions;
using Application.Features.Identity;
using Application.Features.Identity.GetCurrentUser;
using Application.Features.Identity.ListUsers;
using Application.Features.Identity.Login;
using Application.Features.Identity.Mfa;
using Application.Features.Identity.Refresh;
using Application.Features.Identity.Register;
using Application.Features.Identity.Revoke;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(
            typeof(DependencyInjection).Assembly,
            includeInternalTypes: true);
        services.AddScoped<AuthTokenIssuer>();
        services.AddScoped<RegisterUser>();
        services.AddScoped<LoginUser>();
        services.AddScoped<VerifyMfaLogin>();
        services.AddScoped<BeginAuthenticatorSetup>();
        services.AddScoped<ConfirmAuthenticatorSetup>();
        services.AddScoped<DisableAuthenticator>();
        services.AddScoped<RefreshTokens>();
        services.AddScoped<RevokeRefreshToken>();
        services.AddScoped<RevokeAllSessions>();
        services.AddScoped<GetCurrentUser>();
        services.AddScoped<ListUsers>();

        services.AddScoped<CreatePermissionDefinition>();
        services.AddScoped<GetPermissionDefinition>();
        services.AddScoped<ListPermissionDefinitions>();
        services.AddScoped<UpdatePermissionDefinition>();
        services.AddScoped<SetPermissionDefinitionActive>();
        services.AddScoped<CreateUserGroup>();
        services.AddScoped<GetUserGroup>();
        services.AddScoped<ListUserGroups>();
        services.AddScoped<UpdateUserGroup>();
        services.AddScoped<SetUserGroupActive>();
        services.AddScoped<CreateOrganizationUnit>();
        services.AddScoped<GetOrganizationUnit>();
        services.AddScoped<ListOrganizationUnits>();
        services.AddScoped<UpdateOrganizationUnit>();
        services.AddScoped<MoveOrganizationUnit>();
        services.AddScoped<SetOrganizationUnitActive>();
        services.AddScoped<AssignGroupPermission>();
        services.AddScoped<AssignUserToGroup>();
        services.AddScoped<AssignGroupOrganizationUnit>();
        services.AddScoped<RevokeGroupPermission>();
        services.AddScoped<RevokeUserFromGroup>();
        services.AddScoped<RevokeGroupOrganizationUnit>();
        services.AddScoped<ListAuthorizationAuditEvents>();
        services.AddScoped<GetUserAuthorizationContext>();

        // Agent B — organization-unit scope enforcement platform
        services.AddScoped<Features.Authorization.Abstractions.IAuthorizationScopeService, Features.Authorization.AuthorizationScopeService>();
        services.AddScoped<ListAccessibleOrganizationUnits>();

        // Agent C
        services.AddScoped<AssignUserOrganizationUnit>();
        services.AddScoped<RevokeUserOrganizationUnit>();
        services.AddScoped<ListUserOrganizationUnits>();
        services.AddScoped<GetUserCapabilities>();

        // Agent D — delegated administration grant containment
        services.AddScoped<Features.Authorization.Abstractions.IDelegationAuthorityService, Features.Authorization.DelegationAuthorityService>();

        return services;
    }
}
