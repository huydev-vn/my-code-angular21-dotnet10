import { isDevMode, makeEnvironmentProviders } from '@angular/core';
import { provideState, provideStore } from '@ngrx/store';
import { provideStoreDevtools } from '@ngrx/store-devtools';

import { uiFeature } from './ui/ui.feature';

/**
 * Root store only. Feature state (identity, users, …) is registered by the
 * owning feature providers / lazy routes — never imported from `core`.
 */
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
    provideStore(),
    provideState(uiFeature),
    ...devtools,
  ]);
}
