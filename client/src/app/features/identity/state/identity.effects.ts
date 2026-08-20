import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { catchError, exhaustMap, map, of, tap } from 'rxjs';

import { toErrorMessage } from '../../../shared/util/error-message';
import { IdentityApi } from '../data-access/identity.api';
import { IdentityActions } from './identity.actions';

@Injectable()
export class IdentityEffects {
  private readonly actions$ = inject(Actions);
  private readonly api = inject(IdentityApi);
  private readonly router = inject(Router);

  readonly login$ = createEffect(() =>
    this.actions$.pipe(
      ofType(IdentityActions.loginRequested),
      exhaustMap(({ credentials }) =>
        this.api.login(credentials).pipe(
          map((session) => IdentityActions.loginSucceeded({ session })),
          catchError((error: unknown) =>
            of(IdentityActions.loginFailed({ error: toErrorMessage(error, 'Unable to sign in.') })),
          ),
        ),
      ),
    ),
  );

  readonly register$ = createEffect(() =>
    this.actions$.pipe(
      ofType(IdentityActions.registerRequested),
      exhaustMap(({ credentials }) =>
        this.api.register(credentials).pipe(
          map((session) => IdentityActions.registerSucceeded({ session })),
          catchError((error: unknown) =>
            of(
              IdentityActions.registerFailed({
                error: toErrorMessage(error, 'Unable to create an account.'),
              }),
            ),
          ),
        ),
      ),
    ),
  );

  readonly logout$ = createEffect(() =>
    this.actions$.pipe(
      ofType(IdentityActions.logoutRequested),
      exhaustMap(() =>
        this.api.logout().pipe(
          map(() => IdentityActions.logoutSucceeded()),
          catchError((error: unknown) =>
            of(IdentityActions.logoutFailed({ error: toErrorMessage(error, 'Unable to sign out.') })),
          ),
        ),
      ),
    ),
  );

  readonly redirectAfterAuth$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(IdentityActions.loginSucceeded, IdentityActions.registerSucceeded),
        tap(() => void this.router.navigateByUrl('/')),
      ),
    { dispatch: false },
  );

  readonly redirectAfterLogout$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(IdentityActions.logoutSucceeded),
        tap(() => void this.router.navigateByUrl('/auth/login')),
      ),
    { dispatch: false },
  );
}
