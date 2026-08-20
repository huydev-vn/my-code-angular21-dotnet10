import { createActionGroup, emptyProps, props } from '@ngrx/store';

import type { CurrentUser, LoginRequest, RegisterRequest } from '../../../core/auth/current-user.model';

export const IdentityActions = createActionGroup({
  source: 'Identity',
  events: {
    'App Started': emptyProps(),
    'Session Restored': props<{ user: CurrentUser }>(),
    'Session Restore Failed': emptyProps(),
    'Session Invalidated': emptyProps(),
    'Login Requested': props<{ credentials: LoginRequest; returnUrl?: string | null }>(),
    'Login Succeeded': props<{ user: CurrentUser; returnUrl?: string | null }>(),
    'Login Failed': props<{ error: string }>(),
    'Register Requested': props<{ credentials: RegisterRequest; returnUrl?: string | null }>(),
    'Register Succeeded': props<{ user: CurrentUser; returnUrl?: string | null }>(),
    'Register Failed': props<{ error: string }>(),
    'Logout Requested': emptyProps(),
    'Logout Succeeded': emptyProps(),
    'Logout Failed': props<{ error: string }>(),
  },
});
