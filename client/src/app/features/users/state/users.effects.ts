import { Injectable, inject } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Store } from '@ngrx/store';
import { catchError, map, of, switchMap, withLatestFrom } from 'rxjs';

import { toErrorMessage } from '../../../shared/util/error-message';
import { UsersApi } from '../data-access/users.api';
import { UsersActions } from './users.actions';
import { usersFeature } from './users.feature';

@Injectable()
export class UsersEffects {
  private readonly actions$ = inject(Actions);
  private readonly api = inject(UsersApi);
  private readonly store = inject(Store);

  readonly load$ = createEffect(() =>
    this.actions$.pipe(
      ofType(UsersActions.loadRequested),
      withLatestFrom(
        this.store.select(usersFeature.selectPage),
        this.store.select(usersFeature.selectPageSize),
      ),
      switchMap(([{ query }, page, pageSize]) =>
        this.api
          .list({
            page: query?.page ?? page,
            pageSize: query?.pageSize ?? pageSize,
          })
          .pipe(
            map((result) =>
              UsersActions.loadSucceeded({
                users: result.items,
                totalCount: result.totalCount,
                page: result.page,
                pageSize: result.pageSize,
              }),
            ),
            catchError((error: unknown) =>
              of(
                UsersActions.loadFailed({
                  error: toErrorMessage(error, 'Unable to load users.'),
                }),
              ),
            ),
          ),
      ),
    ),
  );

  readonly pageChanged$ = createEffect(() =>
    this.actions$.pipe(
      ofType(UsersActions.pageChanged),
      map(({ page, pageSize }) => UsersActions.loadRequested({ query: { page, pageSize } })),
    ),
  );
}
