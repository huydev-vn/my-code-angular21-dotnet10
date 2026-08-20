import { createFeature, createReducer, createSelector, on } from '@ngrx/store';

import type { CurrentUser } from '../models/identity.models';
import { IdentityActions } from './identity.actions';

export type IdentityStatus = 'anonymous' | 'authenticating' | 'authenticated';

export interface IdentityState {
  user: CurrentUser | null;
  accessToken: string | null;
  status: IdentityStatus;
  error: string | null;
}

export const identityInitialState: IdentityState = {
  user: null,
  accessToken: null,
  status: 'anonymous',
  error: null,
};

const identityReducer = createReducer(
  identityInitialState,
  on(IdentityActions.loginRequested, IdentityActions.registerRequested, (state) => ({
    ...state,
    status: 'authenticating' as const,
    error: null,
  })),
  on(
    IdentityActions.loginSucceeded,
    IdentityActions.registerSucceeded,
    (state, { session }) => ({
      ...state,
      user: session.user,
      accessToken: session.accessToken,
      status: 'authenticated' as const,
      error: null,
    }),
  ),
  on(IdentityActions.loginFailed, IdentityActions.registerFailed, (state, { error }) => ({
    ...state,
    user: null,
    accessToken: null,
    status: 'anonymous' as const,
    error,
  })),
  on(IdentityActions.logoutRequested, (state) => ({
    ...state,
    error: null,
  })),
  on(IdentityActions.logoutSucceeded, () => identityInitialState),
  on(IdentityActions.logoutFailed, (state, { error }) => ({
    ...state,
    error,
  })),
);

export const identityFeature = createFeature({
  name: 'identity',
  reducer: identityReducer,
  extraSelectors: ({ selectStatus, selectUser }) => ({
    selectAuthenticated: createSelector(selectStatus, (status) => status === 'authenticated'),
    selectAuthenticating: createSelector(selectStatus, (status) => status === 'authenticating'),
    selectPermissions: createSelector(selectUser, (user) => user?.permissions ?? []),
  }),
});
