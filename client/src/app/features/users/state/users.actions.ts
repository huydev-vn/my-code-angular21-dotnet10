import { createActionGroup, emptyProps, props } from '@ngrx/store';

import type { PageQuery } from '../../../core/http/page-result.model';
import type { UserSummary } from '../models/user.models';

export const UsersActions = createActionGroup({
  source: 'Users',
  events: {
    'Load Requested': props<{ query?: Partial<PageQuery> }>(),
    'Load Succeeded': props<{
      users: readonly UserSummary[];
      totalCount: number;
      page: number;
      pageSize: number;
    }>(),
    'Load Failed': props<{ error: string }>(),
    'Page Changed': props<{ page: number; pageSize: number }>(),
  },
});
