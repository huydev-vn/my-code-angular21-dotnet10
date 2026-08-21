import { InjectionToken, Signal } from '@angular/core';

import type { AuthStatus } from './auth-status.model';
import type { CurrentUser } from './current-user.model';
import type { SystemPermission } from './system-permissions';

export interface AuthStatePort {
  readonly status: Signal<AuthStatus>;
  readonly user: Signal<CurrentUser | null>;
  readonly error: Signal<string | null>;
  readonly authenticated: Signal<boolean>;
  readonly authenticating: Signal<boolean>;
  readonly initialized: Signal<boolean>;
  hasPermission(permission: SystemPermission): boolean;
}

export interface AuthCommandsPort {
  /** Kick off session restore (call once at app bootstrap). */
  bootstrap(): void;
  logout(): void;
}

export const AUTH_STATE = new InjectionToken<AuthStatePort>('AUTH_STATE');
export const AUTH_COMMANDS = new InjectionToken<AuthCommandsPort>('AUTH_COMMANDS');
