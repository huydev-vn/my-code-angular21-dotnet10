import { Routes } from '@angular/router';

import { guestGuard } from '../../core/auth/auth.guards';
import { LoginPage } from './pages/login-page/login-page';
import { RegisterPage } from './pages/register-page/register-page';

export const identityRoutes: Routes = [
  {
    path: '',
    canActivate: [guestGuard],
    children: [
      { path: 'login', component: LoginPage },
      { path: 'register', component: RegisterPage },
      { path: '', pathMatch: 'full', redirectTo: 'login' },
    ],
  },
];
