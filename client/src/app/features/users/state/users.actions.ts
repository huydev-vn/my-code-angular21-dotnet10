import { createActionGroup, emptyProps, props } from '@ngrx/store';

import type { UserSummary } from '../models/user.models';

export const UsersActions = createActionGroup({
  source: 'Users',
  events: {
    'Load Requested': emptyProps(),
    'Load Succeeded': props<{ users: readonly UserSummary[] }>(),
    'Load Failed': props<{ error: string }>(),
  },
});
