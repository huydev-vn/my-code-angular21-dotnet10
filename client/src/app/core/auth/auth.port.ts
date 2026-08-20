import { InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import type { CurrentUser, LoginRequest, RegisterRequest } from './current-user.model';

export interface AuthPort {
  login(request: LoginRequest): Observable<CurrentUser>;
  register(request: RegisterRequest): Observable<CurrentUser>;
  logout(): Observable<void>;
  restoreSession(): Observable<CurrentUser | null>;
}

export const AUTH_PORT = new InjectionToken<AuthPort>('AUTH_PORT');
