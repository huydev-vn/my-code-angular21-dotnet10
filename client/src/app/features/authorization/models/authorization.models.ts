export interface PermissionDefinition {
  id: string;
  name: string;
  displayName: string;
}

export interface UserGroup {
  id: string;
  name: string;
}

export interface OrganizationUnit {
  id: string;
  name: string;
  parentId: string | null;
}
