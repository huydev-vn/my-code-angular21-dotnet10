import { TokenSession } from './token-session';

describe('TokenSession', () => {
  let session: TokenSession;

  beforeEach(() => {
    session = new TokenSession();
  });

  it('stores the access token in memory only', () => {
    session.setAccessToken({
      accessToken: 'access-1',
      accessTokenExpiresAt: '2030-01-01T00:00:00Z',
    });

    expect(session.getAccessToken()).toBe('access-1');
    expect(session.hasAccessToken()).toBe(true);
    expect(sessionStorage.getItem('auth.refreshToken')).toBeNull();
  });

  it('clears the access token', () => {
    session.setAccessToken({
      accessToken: 'access-1',
      accessTokenExpiresAt: '2030-01-01T00:00:00Z',
    });
    session.clear();

    expect(session.getAccessToken()).toBeNull();
    expect(session.hasAccessToken()).toBe(false);
  });
});
