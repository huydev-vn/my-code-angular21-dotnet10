import { Injectable, inject } from '@angular/core';
import { Store } from '@ngrx/store';

import type { AuthCommandsPort, AuthStatePort } from '../../../core/auth/auth-state.port';
import type { LoginRequest, RegisterRequest } from '../../../core/auth/current-user.model';
import type { SystemPermission } from '../../../core/auth/system-permissions';
import { IdentityActions } from './identity.actions';
import { identityFeature } from './identity.feature';

@Injectable({ providedIn: 'root' })
export class IdentityFacade implements AuthStatePort, AuthCommandsPort {
  private readonly store = inject(Store);

  readonly user = this.store.selectSignal(identityFeature.selectUser);
  readonly status = this.store.selectSignal(identityFeature.selectStatus);
  readonly error = this.store.selectSignal(identityFeature.selectError);
  readonly authenticated = this.store.selectSignal(identityFeature.selectAuthenticated);
  readonly authenticating = this.store.selectSignal(identityFeature.selectAuthenticating);
  readonly initialized = this.store.selectSignal(identityFeature.selectInitialized);

  login(credentials: LoginRequest, returnUrl?: string | null): void {
    this.store.dispatch(IdentityActions.loginRequested({ credentials, returnUrl }));
  }

  register(credentials: RegisterRequest, returnUrl?: string | null): void {
    this.store.dispatch(IdentityActions.registerRequested({ credentials, returnUrl }));
  }

  logout(): void {
    this.store.dispatch(IdentityActions.logoutRequested());
  }

  hasPermission(permission: SystemPermission): boolean {
    return this.user()?.permissions.includes(permission) ?? false;
  }
}
