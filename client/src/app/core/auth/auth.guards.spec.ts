import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { isObservable, firstValueFrom } from 'rxjs';

import { AUTH_STATE, AuthStatePort } from './auth-state.port';
import { authGuard, guestGuard, permissionGuard } from './auth.guards';
import type { AuthStatus } from './auth-status.model';
import type { CurrentUser } from './current-user.model';
import { SystemPermissions } from './system-permissions';

async function runGuard(guardResult: unknown): Promise<unknown> {
  if (isObservable(guardResult)) {
    return firstValueFrom(guardResult as never);
  }

  return guardResult;
}

function createAuthState(status: AuthStatus, permissions: readonly string[] = []): AuthStatePort {
  const user: CurrentUser | null = permissions.length
    ? {
        id: '1',
        email: 'user@local.dev',
        groups: [],
        permissions: permissions as CurrentUser['permissions'],
        accessibleOrganizationUnitIds: [],
      }
    : null;

  return {
    status: (() => status) as AuthStatePort['status'],
    user: (() => user) as AuthStatePort['user'],
    error: (() => null) as AuthStatePort['error'],
    authenticated: (() => status === 'authenticated') as AuthStatePort['authenticated'],
    authenticating: (() => status === 'authenticating') as AuthStatePort['authenticating'],
    initialized: (() => status !== 'initializing') as AuthStatePort['initialized'],
    hasPermission: (permission) => permissions.includes(permission),
  };
}

describe('auth guards', () => {
  const setup = (status: AuthStatus, permissions: readonly string[] = []) => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: AUTH_STATE, useValue: createAuthState(status, permissions) }],
    });
  };

  it('authGuard allows authenticated users', async () => {
    setup('authenticated', [SystemPermissions.UsersRead]);

    const result = await TestBed.runInInjectionContext(async () =>
      runGuard(authGuard({} as never, { url: '/users' } as never)),
    );

    expect(result).toBe(true);
  });

  it('authGuard redirects anonymous users to login with returnUrl', async () => {
    setup('anonymous');

    const result = await TestBed.runInInjectionContext(async () =>
      runGuard(authGuard({} as never, { url: '/users' } as never)),
    );

    const router = TestBed.inject(Router);
    expect(result).toEqual(router.createUrlTree(['/auth/login'], { queryParams: { returnUrl: '/users' } }));
  });

  it('guestGuard redirects authenticated users home', async () => {
    setup('authenticated', [SystemPermissions.UsersRead]);

    const result = await TestBed.runInInjectionContext(async () =>
      runGuard(guestGuard({} as never, {} as never)),
    );

    const router = TestBed.inject(Router);
    expect(result).toEqual(router.createUrlTree(['/']));
  });

  it('permissionGuard redirects unauthorized users to forbidden', async () => {
    setup('authenticated', []);

    const result = await TestBed.runInInjectionContext(async () =>
      runGuard(permissionGuard(SystemPermissions.UsersRead)({} as never, {} as never)),
    );

    const router = TestBed.inject(Router);
    expect(result).toEqual(router.createUrlTree(['/forbidden']));
  });
});
