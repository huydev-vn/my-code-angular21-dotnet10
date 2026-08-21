import { Injectable, inject } from '@angular/core';
import { Store } from '@ngrx/store';

import { AuthorizationActions } from './authorization.actions';
import { authorizationFeature } from './authorization.feature';

@Injectable()
export class AuthorizationFacade {
  private readonly store = inject(Store);

  readonly permissions = this.store.selectSignal(authorizationFeature.selectPermissionItems);
  readonly groups = this.store.selectSignal(authorizationFeature.selectGroupItems);
  readonly organizationUnits = this.store.selectSignal(
    authorizationFeature.selectOrganizationUnitItems,
  );

  readonly permissionsLoading = this.store.selectSignal(
    authorizationFeature.selectPermissionsLoading,
  );
  readonly groupsLoading = this.store.selectSignal(authorizationFeature.selectGroupsLoading);
  readonly organizationUnitsLoading = this.store.selectSignal(
    authorizationFeature.selectOrganizationUnitsLoading,
  );

  readonly permissionsLoaded = this.store.selectSignal(
    authorizationFeature.selectPermissionsLoaded,
  );
  readonly groupsLoaded = this.store.selectSignal(authorizationFeature.selectGroupsLoaded);
  readonly organizationUnitsLoaded = this.store.selectSignal(
    authorizationFeature.selectOrganizationUnitsLoaded,
  );

  readonly permissionsPage = this.store.selectSignal(authorizationFeature.selectPermissionsPage);
  readonly permissionsPageSize = this.store.selectSignal(
    authorizationFeature.selectPermissionsPageSize,
  );
  readonly permissionsTotalCount = this.store.selectSignal(
    authorizationFeature.selectPermissionsTotalCount,
  );

  readonly groupsPage = this.store.selectSignal(authorizationFeature.selectGroupsPage);
  readonly groupsPageSize = this.store.selectSignal(authorizationFeature.selectGroupsPageSize);
  readonly groupsTotalCount = this.store.selectSignal(authorizationFeature.selectGroupsTotalCount);

  readonly organizationUnitsPage = this.store.selectSignal(
    authorizationFeature.selectOrganizationUnitsPage,
  );
  readonly organizationUnitsPageSize = this.store.selectSignal(
    authorizationFeature.selectOrganizationUnitsPageSize,
  );
  readonly organizationUnitsTotalCount = this.store.selectSignal(
    authorizationFeature.selectOrganizationUnitsTotalCount,
  );

  readonly permissionsError = this.store.selectSignal(authorizationFeature.selectPermissionsError);
  readonly groupsError = this.store.selectSignal(authorizationFeature.selectGroupsError);
  readonly organizationUnitsError = this.store.selectSignal(
    authorizationFeature.selectOrganizationUnitsError,
  );

  loadPermissionsIfNeeded(): void {
    if (!this.permissionsLoaded() && !this.permissionsLoading()) {
      this.store.dispatch(AuthorizationActions.loadPermissionsRequested({}));
    }
  }

  loadGroupsIfNeeded(): void {
    if (!this.groupsLoaded() && !this.groupsLoading()) {
      this.store.dispatch(AuthorizationActions.loadGroupsRequested({}));
    }
  }

  loadOrganizationUnitsIfNeeded(): void {
    if (!this.organizationUnitsLoaded() && !this.organizationUnitsLoading()) {
      this.store.dispatch(AuthorizationActions.loadOrganizationUnitsRequested({}));
    }
  }

  reloadPermissions(): void {
    this.store.dispatch(
      AuthorizationActions.loadPermissionsRequested({
        query: {
          page: this.permissionsPage(),
          pageSize: this.permissionsPageSize(),
        },
      }),
    );
  }

  reloadGroups(): void {
    this.store.dispatch(
      AuthorizationActions.loadGroupsRequested({
        query: {
          page: this.groupsPage(),
          pageSize: this.groupsPageSize(),
        },
      }),
    );
  }

  reloadOrganizationUnits(): void {
    this.store.dispatch(
      AuthorizationActions.loadOrganizationUnitsRequested({
        query: {
          page: this.organizationUnitsPage(),
          pageSize: this.organizationUnitsPageSize(),
        },
      }),
    );
  }

  changePermissionsPage(page: number, pageSize: number): void {
    this.store.dispatch(AuthorizationActions.permissionsPageChanged({ page, pageSize }));
  }

  changeGroupsPage(page: number, pageSize: number): void {
    this.store.dispatch(AuthorizationActions.groupsPageChanged({ page, pageSize }));
  }

  changeOrganizationUnitsPage(page: number, pageSize: number): void {
    this.store.dispatch(AuthorizationActions.organizationUnitsPageChanged({ page, pageSize }));
  }
}
