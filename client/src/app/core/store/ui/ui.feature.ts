import { createFeature, createReducer, on } from '@ngrx/store';

import { UiActions } from './ui.actions';

export interface UiState {
  sidenavOpened: boolean;
}

export const uiInitialState: UiState = {
  sidenavOpened: true,
};

const uiReducer = createReducer(
  uiInitialState,
  on(UiActions.toggleSidenav, (state) => ({
    ...state,
    sidenavOpened: !state.sidenavOpened,
  })),
  on(UiActions.openSidenav, (state) => ({ ...state, sidenavOpened: true })),
  on(UiActions.closeSidenav, (state) => ({ ...state, sidenavOpened: false })),
);

export const uiFeature = createFeature({
  name: 'ui',
  reducer: uiReducer,
});
