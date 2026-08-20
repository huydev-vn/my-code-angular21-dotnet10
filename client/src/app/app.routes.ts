import { Routes } from '@angular/router';

import { authGuard } from './core/auth/auth.guards';
import { Shell } from './layout/shell/shell';

export const routes: Routes = [
  {
    path: 'auth',
    loadChildren: () =>
      import('./features/identity/identity.routes').then((module) => module.identityRoutes),
  },
  {
    path: '',
    component: Shell,
    canActivate: [authGuard],
    children: [
      {
        path: '',
        loadChildren: () =>
          import('./features/home/home.routes').then((module) => module.homeRoutes),
      },
      {
        path: 'users',
        loadChildren: () =>
          import('./features/users/users.routes').then((module) => module.usersRoutes),
      },
      {
        path: 'authorization',
        loadChildren: () =>
          import('./features/authorization/authorization.routes').then(
            (module) => module.authorizationRoutes,
          ),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
