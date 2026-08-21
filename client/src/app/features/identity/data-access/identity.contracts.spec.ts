import {
  mapAccessTokenResponse,
  mapUserResponse,
  isSystemPermission,
} from './identity.contracts';

describe('identity contracts', () => {
  it('maps a valid access token response', () => {
    const mapped = mapAccessTokenResponse({
      accessToken: 'tok',
      accessTokenExpiresAt: '2030-01-01T00:00:00Z',
    });

    expect(mapped.accessToken).toBe('tok');
  });

  it('rejects an invalid access token response', () => {
    expect(() => mapAccessTokenResponse({})).toThrow();
  });

  it('maps a user response and filters unknown permissions', () => {
    const mapped = mapUserResponse({
      id: '11111111-1111-1111-1111-111111111111',
      email: 'admin@local.dev',
      createdAt: '2030-01-01T00:00:00Z',
      groups: ['System Administrators'],
      permissions: ['users.read', 'not.a.real.permission'],
      accessibleOrganizationUnitIds: ['00000000-0000-0000-0000-000000000001'],
    });

    expect(mapped.email).toBe('admin@local.dev');
    expect(mapped.permissions).toEqual(['users.read']);
    expect(isSystemPermission('users.read')).toBe(true);
  });
});
