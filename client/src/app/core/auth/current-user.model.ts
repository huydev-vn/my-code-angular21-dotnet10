import type { SystemPermission } from './system-permissions';

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
