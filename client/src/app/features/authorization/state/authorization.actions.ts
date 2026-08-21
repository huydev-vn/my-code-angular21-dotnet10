import { createActionGroup, props } from '@ngrx/store';

import type { PageQuery } from '../../../core/http/page-result.model';
import type {
  OrganizationUnit,
  PermissionDefinition,
  UserGroup,
} from '../models/authorization.models';

export const AuthorizationActions = createActionGroup({
  source: 'Authorization',
  events: {
    'Load Permissions Requested': props<{ query?: Partial<PageQuery> }>(),
    'Load Permissions Succeeded': props<{
      permissions: readonly PermissionDefinition[];
      totalCount: number;
      page: number;
      pageSize: number;
    }>(),
    'Load Permissions Failed': props<{ error: string }>(),
    'Permissions Page Changed': props<{ page: number; pageSize: number }>(),

    'Load Groups Requested': props<{ query?: Partial<PageQuery> }>(),
    'Load Groups Succeeded': props<{
      groups: readonly UserGroup[];
      totalCount: number;
      page: number;
      pageSize: number;
    }>(),
    'Load Groups Failed': props<{ error: string }>(),
    'Groups Page Changed': props<{ page: number; pageSize: number }>(),

    'Load Organization Units Requested': props<{ query?: Partial<PageQuery> }>(),
    'Load Organization Units Succeeded': props<{
      organizationUnits: readonly OrganizationUnit[];
      totalCount: number;
      page: number;
      pageSize: number;
    }>(),
    'Load Organization Units Failed': props<{ error: string }>(),
    'Organization Units Page Changed': props<{ page: number; pageSize: number }>(),
  },
});
