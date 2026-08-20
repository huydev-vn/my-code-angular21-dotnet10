import { identityInitialState, identityFeature } from './identity.feature';
import { IdentityActions } from './identity.actions';
import type { CurrentUser } from '../../../core/auth/current-user.model';
import { SystemPermissions } from '../../../core/auth/system-permissions';

const user: CurrentUser = {
  id: 'user-1',
  email: 'admin@local.dev',
  groups: ['System Administrators'],
  permissions: [SystemPermissions.UsersRead],
  accessibleOrganizationUnitIds: [],
};

describe('identityFeature reducer', () => {
  it('starts initializing', () => {
    expect(identityInitialState.status).toBe('initializing');
    expect(identityInitialState.user).toBeNull();
  });

  it('marks authenticating on login', () => {
    const anonymous = identityFeature.reducer(
      identityInitialState,
      IdentityActions.sessionRestoreFailed(),
    );
    const state = identityFeature.reducer(
      anonymous,
      IdentityActions.loginRequested({
        credentials: { email: 'admin@local.dev', password: 'secret123' },
      }),
    );

    expect(state.status).toBe('authenticating');
    expect(state.error).toBeNull();
  });

  it('stores the user on success without tokens', () => {
    const state = identityFeature.reducer(
      identityInitialState,
      IdentityActions.loginSucceeded({ user }),
    );

    expect(state.status).toBe('authenticated');
    expect(state.user?.email).toBe('admin@local.dev');
    expect('accessToken' in state).toBe(false);
  });

  it('restores session on bootstrap success', () => {
    const state = identityFeature.reducer(
      identityInitialState,
      IdentityActions.sessionRestored({ user }),
    );

    expect(state.status).toBe('authenticated');
    expect(state.user?.email).toBe('admin@local.dev');
  });

  it('clears the session on logout', () => {
    const authenticated = identityFeature.reducer(
      identityInitialState,
      IdentityActions.loginSucceeded({ user }),
    );
    const state = identityFeature.reducer(authenticated, IdentityActions.logoutSucceeded());

    expect(state.status).toBe('anonymous');
    expect(state.user).toBeNull();
  });
});
