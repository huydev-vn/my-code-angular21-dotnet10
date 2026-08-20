import { Injectable } from '@angular/core';
import { Observable, delay, of } from 'rxjs';

import { SystemPermissions } from '../../../core/auth/system-permissions';
import type {
  OrganizationUnit,
  PermissionDefinition,
  UserGroup,
} from '../models/authorization.models';

const permissions: readonly PermissionDefinition[] = Object.values(SystemPermissions).map(
  (name) => ({
    id: name,
    name,
    displayName: name
      .split('.')
      .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
      .join(' / '),
  }),
);

const groups: readonly UserGroup[] = [
  { id: 'g-admin', name: 'System Administrators' },
  { id: 'g-lead', name: 'Leadership' },
  { id: 'g-ops', name: 'Operations' },
];

const organizationUnits: readonly OrganizationUnit[] = [
  { id: 'ou-root', name: 'Headquarters', parentId: null },
  { id: 'ou-eng', name: 'Engineering', parentId: 'ou-root' },
  { id: 'ou-ops', name: 'Operations', parentId: 'ou-root' },
];

@Injectable({ providedIn: 'root' })
export class AuthorizationApi {
  listPermissions(): Observable<readonly PermissionDefinition[]> {
    return of(permissions).pipe(delay(220));
  }

  listGroups(): Observable<readonly UserGroup[]> {
    return of(groups).pipe(delay(220));
  }

  listOrganizationUnits(): Observable<readonly OrganizationUnit[]> {
    return of(organizationUnits).pipe(delay(220));
  }
}
