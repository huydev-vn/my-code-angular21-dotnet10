import { createActionGroup, emptyProps, props } from '@ngrx/store';

import type {
  OrganizationUnit,
  PermissionDefinition,
  UserGroup,
} from '../models/authorization.models';

export const AuthorizationActions = createActionGroup({
  source: 'Authorization',
  events: {
    'Load Catalog Requested': emptyProps(),
    'Load Catalog Succeeded': props<{
      permissions: readonly PermissionDefinition[];
      groups: readonly UserGroup[];
      organizationUnits: readonly OrganizationUnit[];
    }>(),
    'Load Catalog Failed': props<{ error: string }>(),
  },
});
