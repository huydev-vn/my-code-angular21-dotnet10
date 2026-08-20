import { Injectable, inject } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { catchError, exhaustMap, map, of } from 'rxjs';

import { toErrorMessage } from '../../../shared/util/error-message';
import { UsersApi } from '../data-access/users.api';
import { UsersActions } from './users.actions';

@Injectable()
export class UsersEffects {
  private readonly actions$ = inject(Actions);
  private readonly api = inject(UsersApi);

  readonly load$ = createEffect(() =>
    this.actions$.pipe(
      ofType(UsersActions.loadRequested),
      exhaustMap(() =>
        this.api.list().pipe(
          map((users) => UsersActions.loadSucceeded({ users })),
          catchError((error: unknown) =>
            of(UsersActions.loadFailed({ error: toErrorMessage(error, 'Unable to load users.') })),
          ),
        ),
      ),
    ),
  );
}
