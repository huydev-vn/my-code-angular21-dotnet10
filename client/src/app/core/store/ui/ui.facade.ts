import { Injectable, inject } from '@angular/core';
import { Store } from '@ngrx/store';

import { UiActions } from './ui.actions';
import { uiFeature } from './ui.feature';

@Injectable({ providedIn: 'root' })
export class UiFacade {
  private readonly store = inject(Store);

  readonly sidenavOpened = this.store.selectSignal(uiFeature.selectSidenavOpened);

  toggleSidenav(): void {
    this.store.dispatch(UiActions.toggleSidenav());
  }
}
