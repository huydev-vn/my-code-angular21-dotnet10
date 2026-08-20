import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideRouter } from '@angular/router';

import { APP_CONFIG, appConfigValue, provideAppHttp, provideAppStore } from './core';
import { provideIdentityAuth } from './features/identity/provide-identity-auth';
import { provideIdentityUnauthorizedHandler } from './features/identity/provide-identity-unauthorized-handler';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideAnimationsAsync(),
    provideAppHttp(),
    provideAppStore(),
    provideIdentityAuth(),
    provideIdentityUnauthorizedHandler(),
    { provide: APP_CONFIG, useValue: appConfigValue },
  ],
};
