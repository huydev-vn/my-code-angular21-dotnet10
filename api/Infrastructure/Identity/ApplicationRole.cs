using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

/// <summary>
/// ASP.NET Identity role entity retained only for the default Identity schema
/// (AspNetRoles / AspNetUserRoles). Application authorization must use
/// <c>UserGroup</c> + <c>PermissionDefinition</c> — do not introduce RoleManager
/// policies or seed Identity roles for access control.
/// </summary>
public sealed class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole()
    {
    }

    public ApplicationRole(string roleName)
        : base(roleName)
    {
    }
}
