import { createFeature, createReducer, createSelector, on } from '@ngrx/store';

import type { AuthStatus } from '../../../core/auth/auth-status.model';
import type { CurrentUser } from '../../../core/auth/current-user.model';
import { IdentityActions } from './identity.actions';

export interface IdentityState {
  user: CurrentUser | null;
  status: AuthStatus;
  error: string | null;
}

export const identityInitialState: IdentityState = {
  user: null,
  status: 'initializing',
  error: null,
};

const identityReducer = createReducer(
  identityInitialState,
  on(IdentityActions.appStarted, (state) => ({
    ...state,
    status: 'initializing' as const,
    error: null,
  })),
  on(IdentityActions.sessionRestored, (state, { user }) => ({
    ...state,
    user,
    status: 'authenticated' as const,
    error: null,
  })),
  on(IdentityActions.sessionRestoreFailed, IdentityActions.sessionInvalidated, (state) => ({
    ...state,
    user: null,
    status: 'anonymous' as const,
    error: null,
  })),
  on(IdentityActions.loginRequested, IdentityActions.registerRequested, (state) => ({
    ...state,
    status: 'authenticating' as const,
    error: null,
  })),
  on(
    IdentityActions.loginSucceeded,
    IdentityActions.registerSucceeded,
    (state, { user }) => ({
      ...state,
      user,
      status: 'authenticated' as const,
      error: null,
    }),
  ),
  on(IdentityActions.loginFailed, IdentityActions.registerFailed, (state, { error }) => ({
    ...state,
    user: null,
    status: 'anonymous' as const,
    error,
  })),
  on(IdentityActions.logoutRequested, (state) => ({
    ...state,
    error: null,
  })),
  on(IdentityActions.logoutSucceeded, () => ({
    user: null,
    status: 'anonymous' as const,
    error: null,
  })),
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
    selectInitialized: createSelector(selectStatus, (status) => status !== 'initializing'),
    selectPermissions: createSelector(selectUser, (user) => user?.permissions ?? []),
  }),
});
