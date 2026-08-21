import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';

import { SKIP_AUTH_REFRESH } from './auth-http.context';
import { UNAUTHORIZED_HANDLER } from './unauthorized-handler.port';

function isAnonymousAuthUrl(url: string): boolean {
  return /\/identity\/(login|register|refresh|revoke)(\?|$)/.test(url);
}

/**
 * Clears the session on hard 401s after refresh was skipped or already
 * attempted (retry marks {@link SKIP_AUTH_REFRESH}). Must stay *outside*
 * {@link authInterceptor} so a shared refresh can finish first.
 */
export const unauthorizedInterceptor: HttpInterceptorFn = (request, next) => {
  const onUnauthorized = inject(UNAUTHORIZED_HANDLER, { optional: true });

  return next(request).pipe(
    catchError((error: unknown) => {
      if (
        onUnauthorized &&
        error instanceof HttpErrorResponse &&
        error.status === 401 &&
        !isAnonymousAuthUrl(request.url) &&
        request.context.get(SKIP_AUTH_REFRESH)
      ) {
        onUnauthorized();
      }

      return throwError(() => error);
    }),
  );
};
