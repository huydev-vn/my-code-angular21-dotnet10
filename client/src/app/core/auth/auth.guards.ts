import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { map, take } from 'rxjs';

import { identityFeature } from '../../features/identity/state/identity.feature';
import type { SystemPermission } from '../../features/identity/models/identity.models';

export const authGuard: CanActivateFn = () => {
  const store = inject(Store);
  const router = inject(Router);

  return store.select(identityFeature.selectStatus).pipe(
    take(1),
    map((status) => status === 'authenticated' || router.createUrlTree(['/auth/login'])),
  );
};

export const guestGuard: CanActivateFn = () => {
  const store = inject(Store);
  const router = inject(Router);

  return store.select(identityFeature.selectStatus).pipe(
    take(1),
    map((status) => status !== 'authenticated' || router.createUrlTree(['/'])),
  );
};

export function permissionGuard(permission: SystemPermission): CanActivateFn {
  return () => {
    const store = inject(Store);
    const router = inject(Router);

    return store.select(identityFeature.selectUser).pipe(
      take(1),
      map((user) => {
        if (user?.permissions.includes(permission)) {
          return true;
        }

        return router.createUrlTree(['/']);
      }),
    );
  };
}
