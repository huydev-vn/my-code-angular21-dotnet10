using Application.Features.Authorization.Assignments;
using Application.Features.Authorization.GetContext;
using Application.Features.Authorization.Groups;
using Application.Features.Authorization.OrganizationUnits;
using Application.Features.Authorization.Permissions;
using Application.Features.Identity;
using Application.Features.Identity.GetCurrentUser;
using Application.Features.Identity.ListUsers;
using Application.Features.Identity.Login;
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
        services.AddScoped<RefreshTokens>();
        services.AddScoped<RevokeRefreshToken>();
        services.AddScoped<GetCurrentUser>();
        services.AddScoped<ListUsers>();

        services.AddScoped<CreatePermissionDefinition>();
        services.AddScoped<ListPermissionDefinitions>();
        services.AddScoped<CreateUserGroup>();
        services.AddScoped<ListUserGroups>();
        services.AddScoped<CreateOrganizationUnit>();
        services.AddScoped<ListOrganizationUnits>();
        services.AddScoped<AssignGroupPermission>();
        services.AddScoped<AssignUserToGroup>();
        services.AddScoped<AssignGroupOrganizationUnit>();
        services.AddScoped<GetUserAuthorizationContext>();

        return services;
    }
}
