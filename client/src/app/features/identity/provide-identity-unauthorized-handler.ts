import { inject, makeEnvironmentProviders } from '@angular/core';
import { Store } from '@ngrx/store';

import { UNAUTHORIZED_HANDLER } from '../../core/http/unauthorized-handler.port';
import { IdentityActions } from './state/identity.actions';

export function provideIdentityUnauthorizedHandler() {
  return makeEnvironmentProviders([
    {
      provide: UNAUTHORIZED_HANDLER,
      useFactory: () => {
        const store = inject(Store);
        return () => store.dispatch(IdentityActions.sessionInvalidated());
      },
    },
  ]);
}
