import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';

import { APP_CONFIG } from '../../../core/config/app-config';
import type { AuthPort } from '../../../core/auth/auth.port';
import type { CurrentUser, LoginRequest, RegisterRequest } from '../../../core/auth/current-user.model';

@Injectable()
export class IdentityHttpAdapter implements AuthPort {
  private readonly http = inject(HttpClient);
  private readonly config = inject(APP_CONFIG);

  login(request: LoginRequest): Observable<CurrentUser> {
    return this.http
      .post<CurrentUser>(`${this.config.apiBaseUrl}/auth/login`, request, {
        withCredentials: true,
      })
      .pipe(map((user) => user));
  }

  register(request: RegisterRequest): Observable<CurrentUser> {
    return this.http
      .post<CurrentUser>(`${this.config.apiBaseUrl}/auth/register`, request, {
        withCredentials: true,
      })
      .pipe(map((user) => user));
  }

  logout(): Observable<void> {
    return this.http.post<void>(`${this.config.apiBaseUrl}/auth/logout`, null, {
      withCredentials: true,
    });
  }

  restoreSession(): Observable<CurrentUser | null> {
    return this.http.get<CurrentUser | null>(`${this.config.apiBaseUrl}/auth/me`, {
      withCredentials: true,
    });
  }
}
