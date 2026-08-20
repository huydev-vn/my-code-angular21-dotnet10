import { Injectable } from '@angular/core';
import { Observable, delay, of, throwError } from 'rxjs';

import type { AuthPort } from '../../../core/auth/auth.port';
import type { CurrentUser, LoginRequest, RegisterRequest } from '../../../core/auth/current-user.model';
import { SystemPermissions } from '../../../core/auth/system-permissions';

const MOCK_SESSION_KEY = 'mock-auth-session';

const demoUser: CurrentUser = {
  id: '11111111-1111-1111-1111-111111111111',
  email: 'admin@local.dev',
  groups: ['System Administrators'],
  permissions: Object.values(SystemPermissions),
  accessibleOrganizationUnitIds: ['00000000-0000-0000-0000-000000000001'],
};

function readStoredUser(): CurrentUser | null {
  const raw = sessionStorage.getItem(MOCK_SESSION_KEY);
  if (!raw) {
    return null;
  }

  try {
    return JSON.parse(raw) as CurrentUser;
  } catch {
    sessionStorage.removeItem(MOCK_SESSION_KEY);
    return null;
  }
}

function persistUser(user: CurrentUser): void {
  sessionStorage.setItem(MOCK_SESSION_KEY, JSON.stringify(user));
}

function clearStoredUser(): void {
  sessionStorage.removeItem(MOCK_SESSION_KEY);
}

@Injectable()
export class IdentityMockAdapter implements AuthPort {
  login(request: LoginRequest): Observable<CurrentUser> {
    if (!request.email.includes('@')) {
      return throwError(() => new Error('Invalid credentials.')).pipe(delay(250));
    }

    const user: CurrentUser = {
      ...demoUser,
      email: request.email,
    };

    persistUser(user);
    return of(user).pipe(delay(350));
  }

  register(request: RegisterRequest): Observable<CurrentUser> {
    return this.login(request);
  }

  logout(): Observable<void> {
    clearStoredUser();
    return of(undefined).pipe(delay(120));
  }

  restoreSession(): Observable<CurrentUser | null> {
    return of(readStoredUser()).pipe(delay(180));
  }
}
