import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';

import { UNAUTHORIZED_HANDLER } from './unauthorized-handler.port';

export const unauthorizedInterceptor: HttpInterceptorFn = (request, next) => {
  const onUnauthorized = inject(UNAUTHORIZED_HANDLER, { optional: true });

  return next(request).pipe(
    catchError((error: unknown) => {
      if (
        onUnauthorized &&
        error instanceof Object &&
        'status' in error &&
        (error as { status: number }).status === 401
      ) {
        onUnauthorized();
      }

      return throwError(() => error);
    }),
  );
};
