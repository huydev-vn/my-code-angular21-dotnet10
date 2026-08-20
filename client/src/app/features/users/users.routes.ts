import { Routes } from '@angular/router';
import { provideEffects } from '@ngrx/effects';
import { provideState } from '@ngrx/store';

import { permissionGuard } from '../../core/auth/auth.guards';
import { SystemPermissions } from '../identity/models/identity.models';
import { UserListPage } from './pages/user-list-page/user-list-page';
import { UsersEffects } from './state/users.effects';
import { UsersFacade } from './state/users.facade';
import { usersFeature } from './state/users.feature';

export const usersRoutes: Routes = [
  {
    path: '',
    providers: [provideState(usersFeature), provideEffects(UsersEffects), UsersFacade],
    canActivate: [permissionGuard(SystemPermissions.UsersRead)],
    component: UserListPage,
  },
];
