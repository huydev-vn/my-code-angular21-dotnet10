namespace Application.Features.Identity.Authorization;

public static class PermissionPolicies
{
    public const string Prefix = "permission:";

    public static string Name(string permission) => Prefix + permission;
}
