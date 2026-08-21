import { createFeature, createReducer, createSelector, on } from '@ngrx/store';

import {
  createInitialListState,
  listFailed,
  listPageChanged,
  listRequested,
  listSucceeded,
  type ListState,
} from '../../../core/store/list-state';
import type {
  OrganizationUnit,
  PermissionDefinition,
  UserGroup,
} from '../models/authorization.models';
import { AuthorizationActions } from './authorization.actions';

export interface AuthorizationState {
  permissions: ListState<PermissionDefinition>;
  groups: ListState<UserGroup>;
  organizationUnits: ListState<OrganizationUnit>;
}

export const authorizationInitialState: AuthorizationState = {
  permissions: createInitialListState<PermissionDefinition>(),
  groups: createInitialListState<UserGroup>(),
  organizationUnits: createInitialListState<OrganizationUnit>(),
};

const authorizationReducer = createReducer(
  authorizationInitialState,
  on(AuthorizationActions.loadPermissionsRequested, (state, { query }) => ({
    ...state,
    permissions: listRequested(state.permissions, query),
  })),
  on(AuthorizationActions.loadPermissionsSucceeded, (state, action) => ({
    ...state,
    permissions: listSucceeded(state.permissions, {
      items: action.permissions,
      totalCount: action.totalCount,
      page: action.page,
      pageSize: action.pageSize,
    }),
  })),
  on(AuthorizationActions.loadPermissionsFailed, (state, { error }) => ({
    ...state,
    permissions: listFailed(state.permissions, error),
  })),
  on(AuthorizationActions.permissionsPageChanged, (state, { page, pageSize }) => ({
    ...state,
    permissions: listPageChanged(state.permissions, page, pageSize),
  })),

  on(AuthorizationActions.loadGroupsRequested, (state, { query }) => ({
    ...state,
    groups: listRequested(state.groups, query),
  })),
  on(AuthorizationActions.loadGroupsSucceeded, (state, action) => ({
    ...state,
    groups: listSucceeded(state.groups, {
      items: action.groups,
      totalCount: action.totalCount,
      page: action.page,
      pageSize: action.pageSize,
    }),
  })),
  on(AuthorizationActions.loadGroupsFailed, (state, { error }) => ({
    ...state,
    groups: listFailed(state.groups, error),
  })),
  on(AuthorizationActions.groupsPageChanged, (state, { page, pageSize }) => ({
    ...state,
    groups: listPageChanged(state.groups, page, pageSize),
  })),

  on(AuthorizationActions.loadOrganizationUnitsRequested, (state, { query }) => ({
    ...state,
    organizationUnits: listRequested(state.organizationUnits, query),
  })),
  on(AuthorizationActions.loadOrganizationUnitsSucceeded, (state, action) => ({
    ...state,
    organizationUnits: listSucceeded(state.organizationUnits, {
      items: action.organizationUnits,
      totalCount: action.totalCount,
      page: action.page,
      pageSize: action.pageSize,
    }),
  })),
  on(AuthorizationActions.loadOrganizationUnitsFailed, (state, { error }) => ({
    ...state,
    organizationUnits: listFailed(state.organizationUnits, error),
  })),
  on(AuthorizationActions.organizationUnitsPageChanged, (state, { page, pageSize }) => ({
    ...state,
    organizationUnits: listPageChanged(state.organizationUnits, page, pageSize),
  })),
);

export const authorizationFeature = createFeature({
  name: 'authorization',
  reducer: authorizationReducer,
  extraSelectors: ({ selectPermissions, selectGroups, selectOrganizationUnits }) => {
    const selectPermissionItems = createSelector(selectPermissions, (slice) => slice.items);
    const selectPermissionsLoading = createSelector(selectPermissions, (slice) => slice.loading);
    const selectPermissionsLoaded = createSelector(selectPermissions, (slice) => slice.loaded);
    const selectPermissionsError = createSelector(selectPermissions, (slice) => slice.error);
    const selectPermissionsPage = createSelector(selectPermissions, (slice) => slice.page);
    const selectPermissionsPageSize = createSelector(selectPermissions, (slice) => slice.pageSize);
    const selectPermissionsTotalCount = createSelector(
      selectPermissions,
      (slice) => slice.totalCount,
    );

    const selectGroupItems = createSelector(selectGroups, (slice) => slice.items);
    const selectGroupsLoading = createSelector(selectGroups, (slice) => slice.loading);
    const selectGroupsLoaded = createSelector(selectGroups, (slice) => slice.loaded);
    const selectGroupsError = createSelector(selectGroups, (slice) => slice.error);
    const selectGroupsPage = createSelector(selectGroups, (slice) => slice.page);
    const selectGroupsPageSize = createSelector(selectGroups, (slice) => slice.pageSize);
    const selectGroupsTotalCount = createSelector(selectGroups, (slice) => slice.totalCount);

    const selectOrganizationUnitItems = createSelector(
      selectOrganizationUnits,
      (slice) => slice.items,
    );
    const selectOrganizationUnitsLoading = createSelector(
      selectOrganizationUnits,
      (slice) => slice.loading,
    );
    const selectOrganizationUnitsLoaded = createSelector(
      selectOrganizationUnits,
      (slice) => slice.loaded,
    );
    const selectOrganizationUnitsError = createSelector(
      selectOrganizationUnits,
      (slice) => slice.error,
    );
    const selectOrganizationUnitsPage = createSelector(
      selectOrganizationUnits,
      (slice) => slice.page,
    );
    const selectOrganizationUnitsPageSize = createSelector(
      selectOrganizationUnits,
      (slice) => slice.pageSize,
    );
    const selectOrganizationUnitsTotalCount = createSelector(
      selectOrganizationUnits,
      (slice) => slice.totalCount,
    );

    return {
      selectPermissionItems,
      selectPermissionsLoading,
      selectPermissionsLoaded,
      selectPermissionsError,
      selectPermissionsPage,
      selectPermissionsPageSize,
      selectPermissionsTotalCount,
      selectGroupItems,
      selectGroupsLoading,
      selectGroupsLoaded,
      selectGroupsError,
      selectGroupsPage,
      selectGroupsPageSize,
      selectGroupsTotalCount,
      selectOrganizationUnitItems,
      selectOrganizationUnitsLoading,
      selectOrganizationUnitsLoaded,
      selectOrganizationUnitsError,
      selectOrganizationUnitsPage,
      selectOrganizationUnitsPageSize,
      selectOrganizationUnitsTotalCount,
    };
  },
});
