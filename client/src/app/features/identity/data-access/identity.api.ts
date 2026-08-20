import { Injectable } from '@angular/core';
import { Observable, delay, of, throwError } from 'rxjs';

import { SystemPermissions } from '../models/identity.models';
import type {
  AuthSession,
  CurrentUser,
  LoginRequest,
  RegisterRequest,
} from '../models/identity.models';

const demoUser: CurrentUser = {
  id: '11111111-1111-1111-1111-111111111111',
  email: 'admin@local.dev',
  groups: ['System Administrators'],
  permissions: Object.values(SystemPermissions),
  accessibleOrganizationUnitIds: ['00000000-0000-0000-0000-000000000001'],
};

function createSession(email: string): AuthSession {
  const now = Date.now();

  return {
    accessToken: 'mock-access-token',
    accessTokenExpiresAt: new Date(now + 15 * 60 * 1000).toISOString(),
    refreshToken: 'mock-refresh-token',
    refreshTokenExpiresAt: new Date(now + 14 * 24 * 60 * 60 * 1000).toISOString(),
    user: {
      ...demoUser,
      email,
    },
  };
}

@Injectable({ providedIn: 'root' })
export class IdentityApi {
  login(request: LoginRequest): Observable<AuthSession> {
    if (!request.email.includes('@')) {
      return throwError(() => new Error('Invalid credentials.')).pipe(delay(250));
    }

    return of(createSession(request.email)).pipe(delay(350));
  }

  register(request: RegisterRequest): Observable<AuthSession> {
    return this.login(request);
  }

  logout(): Observable<void> {
    return of(undefined).pipe(delay(120));
  }
}
