import { createFeature, createReducer, on } from '@ngrx/store';

import type {
  OrganizationUnit,
  PermissionDefinition,
  UserGroup,
} from '../models/authorization.models';
import { AuthorizationActions } from './authorization.actions';

export interface AuthorizationState {
  permissions: readonly PermissionDefinition[];
  groups: readonly UserGroup[];
  organizationUnits: readonly OrganizationUnit[];
  loading: boolean;
  loaded: boolean;
  error: string | null;
}

export const authorizationInitialState: AuthorizationState = {
  permissions: [],
  groups: [],
  organizationUnits: [],
  loading: false,
  loaded: false,
  error: null,
};

const authorizationReducer = createReducer(
  authorizationInitialState,
  on(AuthorizationActions.loadCatalogRequested, (state) => ({
    ...state,
    loading: true,
    error: null,
  })),
  on(AuthorizationActions.loadCatalogSucceeded, (state, payload) => ({
    ...state,
    ...payload,
    loading: false,
    loaded: true,
    error: null,
  })),
  on(AuthorizationActions.loadCatalogFailed, (state, { error }) => ({
    ...state,
    loading: false,
    error,
  })),
);

export const authorizationFeature = createFeature({
  name: 'authorization',
  reducer: authorizationReducer,
});
