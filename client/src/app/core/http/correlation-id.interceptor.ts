import { HttpInterceptorFn } from '@angular/common/http';

const CORRELATION_HEADER = 'X-Correlation-Id';

export const correlationIdInterceptor: HttpInterceptorFn = (request, next) => {
  if (request.headers.has(CORRELATION_HEADER)) {
    return next(request);
  }

  return next(
    request.clone({
      setHeaders: {
        [CORRELATION_HEADER]: crypto.randomUUID(),
      },
    }),
  );
};
