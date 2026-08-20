export const SystemPermissions = {
  UsersRead: 'users.read',
  AuthorizationPermissionsRead: 'authorization.permissions.read',
  AuthorizationPermissionsWrite: 'authorization.permissions.write',
  AuthorizationGroupsRead: 'authorization.groups.read',
  AuthorizationGroupsWrite: 'authorization.groups.write',
  AuthorizationOrganizationUnitsRead: 'authorization.organization-units.read',
  AuthorizationOrganizationUnitsWrite: 'authorization.organization-units.write',
} as const;

export type SystemPermission = (typeof SystemPermissions)[keyof typeof SystemPermissions];
