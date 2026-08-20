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
    path: 'forbidden',
    loadComponent: () =>
      import('./features/errors/pages/forbidden-page/forbidden-page').then(
        (module) => module.ForbiddenPage,
      ),
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
  {
    path: '**',
    loadComponent: () =>
      import('./features/errors/pages/not-found-page/not-found-page').then(
        (module) => module.NotFoundPage,
      ),
  },
];
