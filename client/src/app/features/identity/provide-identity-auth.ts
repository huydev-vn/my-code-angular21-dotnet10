import { makeEnvironmentProviders } from '@angular/core';
import { provideEffects } from '@ngrx/effects';
import { provideState } from '@ngrx/store';

import { AUTH_PORT } from '../../core/auth/auth.port';
import { AUTH_COMMANDS, AUTH_STATE } from '../../core/auth/auth-state.port';
import { APP_CONFIG, type AppConfig } from '../../core/config/app-config';
import { IdentityHttpAdapter } from './data-access/identity-http.adapter';
import { IdentityMockAdapter } from './data-access/identity-mock.adapter';
import { IdentityEffects } from './state/identity.effects';
import { IdentityFacade } from './state/identity.facade';
import { identityFeature } from './state/identity.feature';

function provideAuthPort() {
  return {
    provide: AUTH_PORT,
    deps: [APP_CONFIG, IdentityMockAdapter, IdentityHttpAdapter],
    useFactory: (
      config: AppConfig,
      mockAdapter: IdentityMockAdapter,
      httpAdapter: IdentityHttpAdapter,
    ) => (config.useMockAuth ? mockAdapter : httpAdapter),
  };
}

export function provideIdentityAuth() {
  return makeEnvironmentProviders([
    IdentityMockAdapter,
    IdentityHttpAdapter,
    provideAuthPort(),
    provideState(identityFeature),
    provideEffects(IdentityEffects),
    { provide: AUTH_STATE, useExisting: IdentityFacade },
    { provide: AUTH_COMMANDS, useExisting: IdentityFacade },
  ]);
}
