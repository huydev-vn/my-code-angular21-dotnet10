import { Injectable, inject } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Store } from '@ngrx/store';
import { catchError, map, of, switchMap, withLatestFrom } from 'rxjs';

import { toErrorMessage } from '../../../shared/util/error-message';
import { AuthorizationApi } from '../data-access/authorization.api';
import { AuthorizationActions } from './authorization.actions';
import { authorizationFeature } from './authorization.feature';

@Injectable()
export class AuthorizationEffects {
  private readonly actions$ = inject(Actions);
  private readonly api = inject(AuthorizationApi);
  private readonly store = inject(Store);

  readonly loadPermissions$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthorizationActions.loadPermissionsRequested),
      withLatestFrom(
        this.store.select(authorizationFeature.selectPermissionsPage),
        this.store.select(authorizationFeature.selectPermissionsPageSize),
      ),
      switchMap(([{ query }, page, pageSize]) =>
        this.api
          .listPermissions({
            page: query?.page ?? page,
            pageSize: query?.pageSize ?? pageSize,
          })
          .pipe(
            map((result) =>
              AuthorizationActions.loadPermissionsSucceeded({
                permissions: result.items,
                totalCount: result.totalCount,
                page: result.page,
                pageSize: result.pageSize,
              }),
            ),
            catchError((error: unknown) =>
              of(
                AuthorizationActions.loadPermissionsFailed({
                  error: toErrorMessage(error, 'Unable to load permissions.'),
                }),
              ),
            ),
          ),
      ),
    ),
  );

  readonly permissionsPageChanged$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthorizationActions.permissionsPageChanged),
      map(({ page, pageSize }) =>
        AuthorizationActions.loadPermissionsRequested({ query: { page, pageSize } }),
      ),
    ),
  );

  readonly loadGroups$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthorizationActions.loadGroupsRequested),
      withLatestFrom(
        this.store.select(authorizationFeature.selectGroupsPage),
        this.store.select(authorizationFeature.selectGroupsPageSize),
      ),
      switchMap(([{ query }, page, pageSize]) =>
        this.api
          .listGroups({
            page: query?.page ?? page,
            pageSize: query?.pageSize ?? pageSize,
          })
          .pipe(
            map((result) =>
              AuthorizationActions.loadGroupsSucceeded({
                groups: result.items,
                totalCount: result.totalCount,
                page: result.page,
                pageSize: result.pageSize,
              }),
            ),
            catchError((error: unknown) =>
              of(
                AuthorizationActions.loadGroupsFailed({
                  error: toErrorMessage(error, 'Unable to load groups.'),
                }),
              ),
            ),
          ),
      ),
    ),
  );

  readonly groupsPageChanged$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthorizationActions.groupsPageChanged),
      map(({ page, pageSize }) =>
        AuthorizationActions.loadGroupsRequested({ query: { page, pageSize } }),
      ),
    ),
  );

  readonly loadOrganizationUnits$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthorizationActions.loadOrganizationUnitsRequested),
      withLatestFrom(
        this.store.select(authorizationFeature.selectOrganizationUnitsPage),
        this.store.select(authorizationFeature.selectOrganizationUnitsPageSize),
      ),
      switchMap(([{ query }, page, pageSize]) =>
        this.api
          .listOrganizationUnits({
            page: query?.page ?? page,
            pageSize: query?.pageSize ?? pageSize,
          })
          .pipe(
            map((result) =>
              AuthorizationActions.loadOrganizationUnitsSucceeded({
                organizationUnits: result.items,
                totalCount: result.totalCount,
                page: result.page,
                pageSize: result.pageSize,
              }),
            ),
            catchError((error: unknown) =>
              of(
                AuthorizationActions.loadOrganizationUnitsFailed({
                  error: toErrorMessage(error, 'Unable to load organization units.'),
                }),
              ),
            ),
          ),
      ),
    ),
  );

  readonly organizationUnitsPageChanged$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthorizationActions.organizationUnitsPageChanged),
      map(({ page, pageSize }) =>
        AuthorizationActions.loadOrganizationUnitsRequested({ query: { page, pageSize } }),
      ),
    ),
  );
}
