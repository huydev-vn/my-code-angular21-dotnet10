import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';

import { correlationIdInterceptor } from './correlation-id.interceptor';
import { unauthorizedInterceptor } from './unauthorized.interceptor';

export function provideAppHttp() {
  return provideHttpClient(
    withFetch(),
    withInterceptors([correlationIdInterceptor, unauthorizedInterceptor]),
  );
}
