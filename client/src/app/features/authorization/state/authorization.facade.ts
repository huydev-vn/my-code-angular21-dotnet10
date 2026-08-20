import { Injectable, inject } from '@angular/core';
import { Store } from '@ngrx/store';

import { AuthorizationActions } from './authorization.actions';
import { authorizationFeature } from './authorization.feature';

@Injectable()
export class AuthorizationFacade {
  private readonly store = inject(Store);

  readonly permissions = this.store.selectSignal(authorizationFeature.selectPermissions);
  readonly groups = this.store.selectSignal(authorizationFeature.selectGroups);
  readonly organizationUnits = this.store.selectSignal(
    authorizationFeature.selectOrganizationUnits,
  );
  readonly loading = this.store.selectSignal(authorizationFeature.selectLoading);
  readonly loaded = this.store.selectSignal(authorizationFeature.selectLoaded);
  readonly error = this.store.selectSignal(authorizationFeature.selectError);

  loadCatalogIfNeeded(): void {
    if (!this.loaded() && !this.loading()) {
      this.store.dispatch(AuthorizationActions.loadCatalogRequested());
    }
  }

  reloadCatalog(): void {
    this.store.dispatch(AuthorizationActions.loadCatalogRequested());
  }
}
