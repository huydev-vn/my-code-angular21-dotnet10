import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';

import { APP_CONFIG } from '../../../core/config/app-config';
import { TokenSession } from '../../../core/auth/token-session';
import { IdentityHttpAdapter } from './identity-http.adapter';

describe('IdentityHttpAdapter', () => {
  let adapter: IdentityHttpAdapter;
  let httpTesting: HttpTestingController;
  let tokens: TokenSession;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        IdentityHttpAdapter,
        TokenSession,
        {
          provide: APP_CONFIG,
          useValue: {
            production: false,
            apiBaseUrl: '/api',
            useMockAuth: false,
          },
        },
      ],
    });

    adapter = TestBed.inject(IdentityHttpAdapter);
    httpTesting = TestBed.inject(HttpTestingController);
    tokens = TestBed.inject(TokenSession);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('logs in, stores the access token, and loads the current user', async () => {
    const pending = firstValueFrom(
      adapter.login({ email: 'admin@local.dev', password: 'Password1!' }),
    );

    const login = httpTesting.expectOne('/api/identity/login');
    expect(login.request.withCredentials).toBe(true);
    login.flush({
      accessToken: 'access-1',
      accessTokenExpiresAt: '2030-01-01T00:00:00Z',
    });

    const me = httpTesting.expectOne('/api/identity/me');
    me.flush({
      id: '11111111-1111-1111-1111-111111111111',
      email: 'admin@local.dev',
      createdAt: '2030-01-01T00:00:00Z',
      groups: ['System Administrators'],
      permissions: ['users.read'],
      accessibleOrganizationUnitIds: ['00000000-0000-0000-0000-000000000001'],
    });

    const user = await pending;
    expect(user.email).toBe('admin@local.dev');
    expect(tokens.getAccessToken()).toBe('access-1');
  });

  it('revokes the refresh cookie on logout and clears memory', async () => {
    tokens.setAccessToken({
      accessToken: 'access-1',
      accessTokenExpiresAt: '2030-01-01T00:00:00Z',
    });

    const pending = firstValueFrom(adapter.logout());
    const revoke = httpTesting.expectOne('/api/identity/revoke');
    expect(revoke.request.withCredentials).toBe(true);
    revoke.flush(null, { status: 204, statusText: 'No Content' });
    await pending;

    expect(tokens.getAccessToken()).toBeNull();
  });
});
