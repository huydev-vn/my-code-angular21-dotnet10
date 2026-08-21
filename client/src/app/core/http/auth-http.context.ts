import { HttpContextToken } from '@angular/common/http';

/** Skip bearer attach and refresh-retry for anonymous auth endpoints. */
export const SKIP_AUTH_INTERCEPTOR = new HttpContextToken<boolean>(() => false);

/** Skip refresh-retry (used for the refresh call itself). */
export const SKIP_AUTH_REFRESH = new HttpContextToken<boolean>(() => false);
