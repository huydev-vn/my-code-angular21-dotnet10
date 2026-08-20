import { EntityState, createEntityAdapter } from '@ngrx/entity';
import { createFeature, createReducer, on } from '@ngrx/store';

import type { UserSummary } from '../models/user.models';
import { UsersActions } from './users.actions';

export interface UsersState extends EntityState<UserSummary> {
  loading: boolean;
  loaded: boolean;
  error: string | null;
}

export const usersAdapter = createEntityAdapter<UserSummary>();

export const usersInitialState: UsersState = usersAdapter.getInitialState({
  loading: false,
  loaded: false,
  error: null,
});

const usersReducer = createReducer(
  usersInitialState,
  on(UsersActions.loadRequested, (state) => ({
    ...state,
    loading: true,
    error: null,
  })),
  on(UsersActions.loadSucceeded, (state, { users }) =>
    usersAdapter.setAll([...users], {
      ...state,
      loading: false,
      loaded: true,
      error: null,
    }),
  ),
  on(UsersActions.loadFailed, (state, { error }) => ({
    ...state,
    loading: false,
    error,
  })),
);

export const usersFeature = createFeature({
  name: 'users',
  reducer: usersReducer,
  extraSelectors: ({ selectUsersState }) => ({
    ...usersAdapter.getSelectors(selectUsersState),
  }),
});
