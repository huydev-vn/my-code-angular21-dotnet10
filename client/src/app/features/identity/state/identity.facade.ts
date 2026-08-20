import { Injectable, inject } from '@angular/core';
import { Store } from '@ngrx/store';

import type { LoginRequest, RegisterRequest, SystemPermission } from '../models/identity.models';
import { IdentityActions } from './identity.actions';
import { identityFeature } from './identity.feature';

@Injectable({ providedIn: 'root' })
export class IdentityFacade {
  private readonly store = inject(Store);

  readonly user = this.store.selectSignal(identityFeature.selectUser);
  readonly status = this.store.selectSignal(identityFeature.selectStatus);
  readonly error = this.store.selectSignal(identityFeature.selectError);
  readonly authenticated = this.store.selectSignal(identityFeature.selectAuthenticated);
  readonly authenticating = this.store.selectSignal(identityFeature.selectAuthenticating);

  login(credentials: LoginRequest): void {
    this.store.dispatch(IdentityActions.loginRequested({ credentials }));
  }

  register(credentials: RegisterRequest): void {
    this.store.dispatch(IdentityActions.registerRequested({ credentials }));
  }

  logout(): void {
    this.store.dispatch(IdentityActions.logoutRequested());
  }

  hasPermission(permission: string): boolean {
    return this.user()?.permissions.includes(permission as SystemPermission) ?? false;
  }
}
