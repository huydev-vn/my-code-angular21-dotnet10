import {
  HttpBackend,
  HttpClient,
  HttpErrorResponse,
  HttpInterceptorFn,
} from '@angular/common/http';
import { inject } from '@angular/core';
import {
  Observable,
  catchError,
  finalize,
  shareReplay,
  switchMap,
  throwError,
} from 'rxjs';

import { TokenSession } from '../auth/token-session';
import { APP_CONFIG } from '../config/app-config';
import { SKIP_AUTH_INTERCEPTOR, SKIP_AUTH_REFRESH } from './auth-http.context';
import { UNAUTHORIZED_HANDLER } from './unauthorized-handler.port';

interface AccessTokenResponseDto {
  accessToken: string;
  accessTokenExpiresAt: string;
}

let refreshInFlight$: Observable<AccessTokenResponseDto> | null = null;

function isAnonymousAuthUrl(url: string): boolean {
  return /\/identity\/(login|register|refresh|revoke)(\?|$)/.test(url);
}

/**
 * Attaches the Bearer access token and, on 401, rotates once via the
 * HttpOnly refresh cookie before failing the request.
 *
 * Must sit innermost among auth-related interceptors so it sees 401s
 * before any session-invalidation handler runs.
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const tokens = inject(TokenSession);
  const config = inject(APP_CONFIG);
  const httpBackend = inject(HttpBackend);
  const onUnauthorized = inject(UNAUTHORIZED_HANDLER, { optional: true });

  if (
    request.context.get(SKIP_AUTH_INTERCEPTOR) ||
    isAnonymousAuthUrl(request.url)
  ) {
    return next(request);
  }

  const accessToken = tokens.getAccessToken();
  const authedRequest = accessToken
    ? request.clone({
        setHeaders: { Authorization: `Bearer ${accessToken}` },
      })
    : request;

  return next(authedRequest).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401) {
        return throwError(() => error);
      }

      if (request.context.get(SKIP_AUTH_REFRESH)) {
        return throwError(() => error);
      }

      if (!refreshInFlight$) {
        const bareHttp = new HttpClient(httpBackend);
        refreshInFlight$ = bareHttp
          .post<AccessTokenResponseDto>(
            `${config.apiBaseUrl}/identity/refresh`,
            {},
            { withCredentials: true },
          )
          .pipe(
            finalize(() => {
              refreshInFlight$ = null;
            }),
            shareReplay({ bufferSize: 1, refCount: true }),
          );
      }

      return refreshInFlight$.pipe(
        switchMap((response) => {
          tokens.setAccessToken(response);
          return next(
            request.clone({
              setHeaders: { Authorization: `Bearer ${response.accessToken}` },
              context: request.context.set(SKIP_AUTH_REFRESH, true),
            }),
          );
        }),
        catchError((refreshError: unknown) => {
          tokens.clear();
          onUnauthorized?.();
          return throwError(() => refreshError);
        }),
      );
    }),
  );
};
