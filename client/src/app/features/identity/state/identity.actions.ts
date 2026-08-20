import { createActionGroup, emptyProps, props } from '@ngrx/store';

import type { AuthSession, LoginRequest, RegisterRequest } from '../models/identity.models';

export const IdentityActions = createActionGroup({
  source: 'Identity',
  events: {
    'Login Requested': props<{ credentials: LoginRequest }>(),
    'Login Succeeded': props<{ session: AuthSession }>(),
    'Login Failed': props<{ error: string }>(),
    'Register Requested': props<{ credentials: RegisterRequest }>(),
    'Register Succeeded': props<{ session: AuthSession }>(),
    'Register Failed': props<{ error: string }>(),
    'Logout Requested': emptyProps(),
    'Logout Succeeded': emptyProps(),
    'Logout Failed': props<{ error: string }>(),
  },
});
