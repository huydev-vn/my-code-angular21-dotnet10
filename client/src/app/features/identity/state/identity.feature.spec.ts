import { identityInitialState, identityFeature } from './identity.feature';
import { IdentityActions } from './identity.actions';
import type { AuthSession } from '../models/identity.models';
import { SystemPermissions } from '../models/identity.models';

const session: AuthSession = {
  accessToken: 'token',
  accessTokenExpiresAt: '2026-01-01T00:15:00.000Z',
  refreshToken: 'refresh',
  refreshTokenExpiresAt: '2026-01-15T00:00:00.000Z',
  user: {
    id: 'user-1',
    email: 'admin@local.dev',
    groups: ['System Administrators'],
    permissions: [SystemPermissions.UsersRead],
    accessibleOrganizationUnitIds: [],
  },
};

describe('identityFeature reducer', () => {
  it('starts anonymous', () => {
    expect(identityInitialState.status).toBe('anonymous');
    expect(identityInitialState.user).toBeNull();
  });

  it('marks authenticating on login', () => {
    const state = identityFeature.reducer(
      identityInitialState,
      IdentityActions.loginRequested({
        credentials: { email: 'admin@local.dev', password: 'secret123' },
      }),
    );

    expect(state.status).toBe('authenticating');
    expect(state.error).toBeNull();
  });

  it('stores the session on success', () => {
    const state = identityFeature.reducer(
      identityInitialState,
      IdentityActions.loginSucceeded({ session }),
    );

    expect(state.status).toBe('authenticated');
    expect(state.user?.email).toBe('admin@local.dev');
    expect(state.accessToken).toBe('token');
  });

  it('clears the session on logout', () => {
    const authenticated = identityFeature.reducer(
      identityInitialState,
      IdentityActions.loginSucceeded({ session }),
    );
    const state = identityFeature.reducer(authenticated, IdentityActions.logoutSucceeded());

    expect(state).toEqual(identityInitialState);
  });
});
