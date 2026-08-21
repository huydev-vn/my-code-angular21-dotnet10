import { AuthorizationActions } from './authorization.actions';
import {
  authorizationFeature,
  authorizationInitialState,
} from './authorization.feature';

describe('authorizationFeature reducer', () => {
  it('loads permissions into an isolated list slice', () => {
    const loading = authorizationFeature.reducer(
      authorizationInitialState,
      AuthorizationActions.loadPermissionsRequested({ query: { page: 1, pageSize: 20 } }),
    );

    expect(loading.permissions.loading).toBe(true);
    expect(loading.groups.loading).toBe(false);

    const loaded = authorizationFeature.reducer(
      loading,
      AuthorizationActions.loadPermissionsSucceeded({
        permissions: [
          {
            id: '1',
            name: 'users.read',
            displayName: 'Users read',
            module: 'users',
            action: 'read',
            isActive: true,
          },
        ],
        totalCount: 1,
        page: 1,
        pageSize: 20,
      }),
    );

    expect(loaded.permissions.loading).toBe(false);
    expect(loaded.permissions.loaded).toBe(true);
    expect(loaded.permissions.items).toHaveLength(1);
    expect(loaded.permissions.totalCount).toBe(1);
    expect(loaded.groups.items).toHaveLength(0);
  });

  it('keeps group errors separate from permission errors', () => {
    const failed = authorizationFeature.reducer(
      authorizationInitialState,
      AuthorizationActions.loadGroupsFailed({ error: 'groups down' }),
    );

    expect(failed.groups.error).toBe('groups down');
    expect(failed.permissions.error).toBeNull();
  });
});
