import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, finalize, map, of, switchMap, throwError } from 'rxjs';

import { APP_CONFIG } from '../../../core/config/app-config';
import type { AuthPort } from '../../../core/auth/auth.port';
import type { CurrentUser, LoginRequest, RegisterRequest } from '../../../core/auth/current-user.model';
import { TokenSession } from '../../../core/auth/token-session';
import { SKIP_AUTH_INTERCEPTOR, SKIP_AUTH_REFRESH } from '../../../core/http/auth-http.context';
import {
  mapAccessTokenResponse,
  mapUserResponse,
} from './identity.contracts';

@Injectable()
export class IdentityHttpAdapter implements AuthPort {
  private readonly http = inject(HttpClient);
  private readonly config = inject(APP_CONFIG);
  private readonly tokens = inject(TokenSession);

  private get identityUrl(): string {
    return `${this.config.apiBaseUrl}/identity`;
  }

  private get skipAuthContext(): HttpContext {
    return new HttpContext()
      .set(SKIP_AUTH_INTERCEPTOR, true)
      .set(SKIP_AUTH_REFRESH, true);
  }

  login(request: LoginRequest): Observable<CurrentUser> {
    return this.http
      .post(`${this.identityUrl}/login`, request, {
        context: this.skipAuthContext,
        withCredentials: true,
      })
      .pipe(
        map(mapAccessTokenResponse),
        switchMap((auth) => {
          this.tokens.setAccessToken(auth);
          return this.fetchCurrentUser();
        }),
      );
  }

  register(request: RegisterRequest): Observable<CurrentUser> {
    return this.http
      .post(`${this.identityUrl}/register`, request, {
        context: this.skipAuthContext,
        withCredentials: true,
      })
      .pipe(
        map(mapAccessTokenResponse),
        switchMap((auth) => {
          this.tokens.setAccessToken(auth);
          return this.fetchCurrentUser();
        }),
      );
  }

  logout(): Observable<void> {
    return this.http
      .post<void>(`${this.identityUrl}/revoke`, {}, {
        context: this.skipAuthContext,
        withCredentials: true,
      })
      .pipe(
        map(() => undefined),
        catchError(() => of(undefined)),
        finalize(() => this.tokens.clear()),
      );
  }

  restoreSession(): Observable<CurrentUser | null> {
    if (this.tokens.hasAccessToken()) {
      return this.fetchCurrentUser().pipe(catchError(() => this.restoreViaRefresh()));
    }

    return this.restoreViaRefresh();
  }

  private restoreViaRefresh(): Observable<CurrentUser | null> {
    return this.http
      .post(`${this.identityUrl}/refresh`, {}, {
        context: this.skipAuthContext,
        withCredentials: true,
      })
      .pipe(
        map(mapAccessTokenResponse),
        switchMap((auth) => {
          this.tokens.setAccessToken(auth);
          return this.fetchCurrentUser();
        }),
        catchError(() => {
          this.tokens.clear();
          return of(null);
        }),
      );
  }

  private fetchCurrentUser(): Observable<CurrentUser> {
    return this.http.get(`${this.identityUrl}/me`, { withCredentials: true }).pipe(
      map(mapUserResponse),
      catchError((error: unknown) => {
        this.tokens.clear();
        return throwError(() => error);
      }),
    );
  }
}
