import { isDevMode, makeEnvironmentProviders } from '@angular/core';
import { provideEffects } from '@ngrx/effects';
import { provideRouterStore, routerReducer } from '@ngrx/router-store';
import { provideState, provideStore } from '@ngrx/store';
import { provideStoreDevtools } from '@ngrx/store-devtools';

import { IdentityEffects } from '../../features/identity/state/identity.effects';
import { identityFeature } from '../../features/identity/state/identity.feature';
import { uiFeature } from './ui/ui.feature';

export function provideAppStore() {
  const devtools = isDevMode()
    ? [
        provideStoreDevtools({
          maxAge: 25,
          logOnly: false,
          autoPause: true,
          trace: false,
          connectInZone: true,
        }),
      ]
    : [];

  return makeEnvironmentProviders([
    provideStore({
      router: routerReducer,
    }),
    provideState(uiFeature),
    provideState(identityFeature),
    provideEffects(IdentityEffects),
    provideRouterStore(),
    ...devtools,
  ]);
}
