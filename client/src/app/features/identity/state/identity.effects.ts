import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { catchError, exhaustMap, map, of, tap } from 'rxjs';

import { AUTH_PORT } from '../../../core/auth/auth.port';
import { mapHttpError } from '../../../core/http/map-http-error';
import { IdentityActions } from './identity.actions';

function sanitizeReturnUrl(returnUrl?: string | null): string {
  if (!returnUrl || !returnUrl.startsWith('/') || returnUrl.startsWith('//')) {
    return '/';
  }

  return returnUrl;
}

@Injectable()
export class IdentityEffects {
  private readonly actions$ = inject(Actions);
  private readonly authPort = inject(AUTH_PORT);
  private readonly router = inject(Router);

  readonly bootstrap$ = createEffect(() =>
    this.actions$.pipe(
      ofType(IdentityActions.appStarted),
      exhaustMap(() =>
        this.authPort.restoreSession().pipe(
          map((user) =>
            user
              ? IdentityActions.sessionRestored({ user })
              : IdentityActions.sessionRestoreFailed(),
          ),
          catchError(() => of(IdentityActions.sessionRestoreFailed())),
        ),
      ),
    ),
  );

  readonly login$ = createEffect(() =>
    this.actions$.pipe(
      ofType(IdentityActions.loginRequested),
      exhaustMap(({ credentials, returnUrl }) =>
        this.authPort.login(credentials).pipe(
          map((user) => IdentityActions.loginSucceeded({ user, returnUrl })),
          catchError((error: unknown) =>
            of(
              IdentityActions.loginFailed({
                error: mapHttpError(error, 'Unable to sign in.'),
              }),
            ),
          ),
        ),
      ),
    ),
  );

  readonly register$ = createEffect(() =>
    this.actions$.pipe(
      ofType(IdentityActions.registerRequested),
      exhaustMap(({ credentials, returnUrl }) =>
        this.authPort.register(credentials).pipe(
          map((user) => IdentityActions.registerSucceeded({ user, returnUrl })),
          catchError((error: unknown) =>
            of(
              IdentityActions.registerFailed({
                error: mapHttpError(error, 'Unable to create an account.'),
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
        this.authPort.logout().pipe(
          map(() => IdentityActions.logoutSucceeded()),
          catchError((error: unknown) =>
            of(
              IdentityActions.logoutFailed({
                error: mapHttpError(error, 'Unable to sign out.'),
              }),
            ),
          ),
        ),
      ),
    ),
  );

  readonly clearRemoteSession$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(IdentityActions.sessionInvalidated),
        exhaustMap(() => this.authPort.logout().pipe(catchError(() => of(undefined)))),
      ),
    { dispatch: false },
  );

  readonly redirectAfterAuth$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(IdentityActions.loginSucceeded, IdentityActions.registerSucceeded),
        tap(({ returnUrl }) => void this.router.navigateByUrl(sanitizeReturnUrl(returnUrl))),
      ),
    { dispatch: false },
  );

  readonly redirectAfterLogout$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(IdentityActions.logoutSucceeded, IdentityActions.sessionInvalidated),
        tap(() => void this.router.navigateByUrl('/auth/login')),
      ),
    { dispatch: false },
  );
}
