import { Injectable, inject } from '@angular/core';
import { Store } from '@ngrx/store';

import { UsersActions } from './users.actions';
import { usersFeature } from './users.feature';

@Injectable()
export class UsersFacade {
  private readonly store = inject(Store);

  readonly users = this.store.selectSignal(usersFeature.selectAll);
  readonly loading = this.store.selectSignal(usersFeature.selectLoading);
  readonly loaded = this.store.selectSignal(usersFeature.selectLoaded);
  readonly error = this.store.selectSignal(usersFeature.selectError);
  readonly page = this.store.selectSignal(usersFeature.selectPage);
  readonly pageSize = this.store.selectSignal(usersFeature.selectPageSize);
  readonly totalCount = this.store.selectSignal(usersFeature.selectTotalCount);

  loadIfNeeded(): void {
    if (!this.loaded() && !this.loading()) {
      this.store.dispatch(UsersActions.loadRequested({}));
    }
  }

  reload(): void {
    this.store.dispatch(
      UsersActions.loadRequested({
        query: { page: this.page(), pageSize: this.pageSize() },
      }),
    );
  }

  changePage(page: number, pageSize: number): void {
    this.store.dispatch(UsersActions.pageChanged({ page, pageSize }));
  }
}
