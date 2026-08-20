import { Injectable, inject } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { catchError, forkJoin, map, of, switchMap } from 'rxjs';

import { toErrorMessage } from '../../../shared/util/error-message';
import { AuthorizationApi } from '../data-access/authorization.api';
import { AuthorizationActions } from './authorization.actions';

@Injectable()
export class AuthorizationEffects {
  private readonly actions$ = inject(Actions);
  private readonly api = inject(AuthorizationApi);

  readonly loadCatalog$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthorizationActions.loadCatalogRequested),
      switchMap(() =>
        forkJoin({
          permissions: this.api.listPermissions(),
          groups: this.api.listGroups(),
          organizationUnits: this.api.listOrganizationUnits(),
        }).pipe(
          map((catalog) => AuthorizationActions.loadCatalogSucceeded(catalog)),
          catchError((error: unknown) =>
            of(
              AuthorizationActions.loadCatalogFailed({
                error: toErrorMessage(error, 'Unable to load authorization catalog.'),
              }),
            ),
          ),
        ),
      ),
    ),
  );
}
