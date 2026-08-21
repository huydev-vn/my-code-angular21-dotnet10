export interface PermissionDefinition {
  id: string;
  name: string;
  displayName: string;
  module: string | null;
  action: string | null;
  isActive: boolean;
}

export interface UserGroup {
  id: string;
  name: string;
  description: string | null;
  isActive: boolean;
}

export interface OrganizationUnit {
  id: string;
  name: string;
  code: string;
  parentId: string | null;
  isActive: boolean;
}
