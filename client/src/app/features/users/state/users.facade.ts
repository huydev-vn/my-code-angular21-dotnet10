import { Injectable, inject } from '@angular/core';
import { Store } from '@ngrx/store';

import { UsersActions } from './users.actions';
import { usersFeature } from './users.feature';

@Injectable()
export class UsersFacade {
  private readonly store = inject(Store);

  readonly users = this.store.selectSignal(usersFeature.selectAll);
  readonly loading = this.store.selectSignal(usersFeature.selectLoading);
  readonly error = this.store.selectSignal(usersFeature.selectError);

  load(): void {
    this.store.dispatch(UsersActions.loadRequested());
  }
}
