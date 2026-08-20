import { UsersActions } from './users.actions';
import { usersFeature, usersInitialState } from './users.feature';

describe('usersFeature reducer', () => {
  it('loads users into the entity adapter', () => {
    const loading = usersFeature.reducer(usersInitialState, UsersActions.loadRequested());
    expect(loading.loading).toBe(true);

    const loaded = usersFeature.reducer(
      loading,
      UsersActions.loadSucceeded({
        users: [{ id: '1', email: 'a@local.dev', groups: ['Ops'] }],
      }),
    );

    expect(loaded.loading).toBe(false);
    expect(loaded.ids).toEqual(['1']);
    expect(loaded.entities['1']?.email).toBe('a@local.dev');
  });
});
