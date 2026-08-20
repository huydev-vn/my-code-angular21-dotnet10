import { provideHttpClient, withInterceptors } from '@angular/common/http';

export function provideAppHttp() {
  return provideHttpClient(withInterceptors([]));
}
