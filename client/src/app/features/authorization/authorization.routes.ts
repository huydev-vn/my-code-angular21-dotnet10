import { Routes } from '@angular/router';
import { provideEffects } from '@ngrx/effects';
import { provideState } from '@ngrx/store';

import { permissionGuard } from '../../core/auth/auth.guards';
import { SystemPermissions } from '../identity/models/identity.models';
import { AuthorizationShell } from './pages/authorization-shell/authorization-shell';
import { GroupListPage } from './pages/group-list-page/group-list-page';
import { OrganizationUnitListPage } from './pages/organization-unit-list-page/organization-unit-list-page';
import { PermissionListPage } from './pages/permission-list-page/permission-list-page';
import { AuthorizationEffects } from './state/authorization.effects';
import { AuthorizationFacade } from './state/authorization.facade';
import { authorizationFeature } from './state/authorization.feature';

export const authorizationRoutes: Routes = [
  {
    path: '',
    component: AuthorizationShell,
    providers: [
      provideState(authorizationFeature),
      provideEffects(AuthorizationEffects),
      AuthorizationFacade,
    ],
    children: [
      {
        path: 'permissions',
        canActivate: [permissionGuard(SystemPermissions.AuthorizationPermissionsRead)],
        component: PermissionListPage,
      },
      {
        path: 'groups',
        canActivate: [permissionGuard(SystemPermissions.AuthorizationGroupsRead)],
        component: GroupListPage,
      },
      {
        path: 'organization-units',
        canActivate: [permissionGuard(SystemPermissions.AuthorizationOrganizationUnitsRead)],
        component: OrganizationUnitListPage,
      },
      { path: '', pathMatch: 'full', redirectTo: 'permissions' },
    ],
  },
];
