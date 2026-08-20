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

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
}

export interface CurrentUser {
  id: string;
  email: string;
  groups: readonly string[];
  permissions: readonly SystemPermission[];
  accessibleOrganizationUnitIds: readonly string[];
}

export interface AuthSession {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  user: CurrentUser;
}
