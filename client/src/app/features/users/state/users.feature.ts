import { EntityState, createEntityAdapter } from '@ngrx/entity';
import { createFeature, createReducer, on } from '@ngrx/store';

import {
  createInitialPagedQueryState,
  pagedQueryFailed,
  pagedQueryPageChanged,
  pagedQueryRequested,
  pagedQuerySucceeded,
  type PagedQueryState,
} from '../../../core/store/list-state';
import type { UserSummary } from '../models/user.models';
import { UsersActions } from './users.actions';

export interface UsersState extends EntityState<UserSummary>, PagedQueryState {}

export const usersAdapter = createEntityAdapter<UserSummary>();

export const usersInitialState: UsersState = usersAdapter.getInitialState(
  createInitialPagedQueryState(20),
);

const usersReducer = createReducer(
  usersInitialState,
  on(UsersActions.loadRequested, (state, { query }) => ({
    ...state,
    ...pagedQueryRequested(state, query),
  })),
  on(UsersActions.pageChanged, (state, { page, pageSize }) => ({
    ...state,
    ...pagedQueryPageChanged(state, page, pageSize),
  })),
  on(UsersActions.loadSucceeded, (state, { users, totalCount, page, pageSize }) =>
    usersAdapter.setAll([...users], {
      ...state,
      ...pagedQuerySucceeded(state, { totalCount, page, pageSize }),
    }),
  ),
  on(UsersActions.loadFailed, (state, { error }) => ({
    ...state,
    ...pagedQueryFailed(state, error),
  })),
);

export const usersFeature = createFeature({
  name: 'users',
  reducer: usersReducer,
  extraSelectors: ({ selectUsersState }) => ({
    ...usersAdapter.getSelectors(selectUsersState),
  }),
});
