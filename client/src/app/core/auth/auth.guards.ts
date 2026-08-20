import { inject, Injector } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { filter, map, take } from 'rxjs';
import { toObservable } from '@angular/core/rxjs-interop';

import { AUTH_STATE, AuthStatePort } from './auth-state.port';
import type { SystemPermission } from './system-permissions';

function waitForInitialized(authState: AuthStatePort, injector: Injector) {
  return toObservable(authState.initialized, { injector }).pipe(
    filter((initialized) => initialized),
    take(1),
    map(() => authState),
  );
}

export const authGuard: CanActivateFn = (_route, state) => {
  const authState = inject(AUTH_STATE);
  const router = inject(Router);
  const injector = inject(Injector);

  return waitForInitialized(authState, injector).pipe(
    map((auth) =>
      auth.authenticated()
        ? true
        : router.createUrlTree(['/auth/login'], {
            queryParams: { returnUrl: state.url },
          }),
    ),
  );
};

export const guestGuard: CanActivateFn = () => {
  const authState = inject(AUTH_STATE);
  const router = inject(Router);
  const injector = inject(Injector);

  return waitForInitialized(authState, injector).pipe(
    map((auth) => !auth.authenticated() || router.createUrlTree(['/'])),
  );
};

export function permissionGuard(permission: SystemPermission): CanActivateFn {
  return () => {
    const authState = inject(AUTH_STATE);
    const router = inject(Router);
    const injector = inject(Injector);

    return waitForInitialized(authState, injector).pipe(
      map((auth) => {
        if (!auth.authenticated()) {
          return router.createUrlTree(['/auth/login']);
        }

        if (auth.hasPermission(permission)) {
          return true;
        }

        return router.createUrlTree(['/forbidden']);
      }),
    );
  };
}
