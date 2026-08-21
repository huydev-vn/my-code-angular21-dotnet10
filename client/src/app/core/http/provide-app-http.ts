import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';

import { authInterceptor } from './auth.interceptor';
import { correlationIdInterceptor } from './correlation-id.interceptor';
import { unauthorizedInterceptor } from './unauthorized.interceptor';

/**
 * Interceptor order (outer → inner):
 * correlationId → unauthorized → auth → backend
 *
 * Response flows the opposite way, so auth sees 401 first, refreshes, and
 * only then does unauthorized/session invalidation run for hard failures.
 */
export function provideAppHttp() {
  return provideHttpClient(
    withFetch(),
    withInterceptors([correlationIdInterceptor, unauthorizedInterceptor, authInterceptor]),
  );
}
