import { InjectionToken } from '@angular/core';

import { environment } from '../../../environments/environment';

export interface AppConfig {
  readonly production: boolean;
  readonly apiBaseUrl: string;
  readonly useMockAuth: boolean;
}

export const APP_CONFIG = new InjectionToken<AppConfig>('APP_CONFIG');

export const appConfigValue: AppConfig = {
  production: environment.production,
  apiBaseUrl: environment.apiBaseUrl,
  useMockAuth: environment.useMockAuth,
};
