import {
  HttpClient,
  HttpErrorResponse,
  provideHttpClient,
  withInterceptors,
} from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';

import { TokenSession } from '../auth/token-session';
import { APP_CONFIG } from '../config/app-config';
import { authInterceptor } from './auth.interceptor';
import { unauthorizedInterceptor } from './unauthorized.interceptor';
import { UNAUTHORIZED_HANDLER } from './unauthorized-handler.port';

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpTesting: HttpTestingController;
  let tokens: TokenSession;
  let onUnauthorized: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    onUnauthorized = vi.fn();

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(
          withInterceptors([unauthorizedInterceptor, authInterceptor]),
        ),
        provideHttpClientTesting(),
        TokenSession,
        {
          provide: APP_CONFIG,
          useValue: {
            production: false,
            apiBaseUrl: '/api',
            useMockAuth: false,
          },
        },
        { provide: UNAUTHORIZED_HANDLER, useValue: onUnauthorized },
      ],
    });

    http = TestBed.inject(HttpClient);
    httpTesting = TestBed.inject(HttpTestingController);
    tokens = TestBed.inject(TokenSession);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('attaches the bearer access token', async () => {
    tokens.setAccessToken({
      accessToken: 'access-token',
      accessTokenExpiresAt: '2030-01-01T00:00:00Z',
    });

    const pending = firstValueFrom(http.get('/api/authorization/permissions'));
    const request = httpTesting.expectOne('/api/authorization/permissions');
    expect(request.request.headers.get('Authorization')).toBe('Bearer access-token');
    request.flush({ items: [], totalCount: 0, page: 1, pageSize: 20 });
    await pending;
    expect(onUnauthorized).not.toHaveBeenCalled();
  });

  it('refreshes once on 401 and retries the original request without invalidating', async () => {
    tokens.setAccessToken({
      accessToken: 'expired',
      accessTokenExpiresAt: '2020-01-01T00:00:00Z',
    });

    const pending = firstValueFrom(http.get('/api/identity/users'));
    const first = httpTesting.expectOne('/api/identity/users');
    first.flush(null, { status: 401, statusText: 'Unauthorized' });

    const refresh = httpTesting.expectOne('/api/identity/refresh');
    expect(refresh.request.withCredentials).toBe(true);
    refresh.flush({
      accessToken: 'next-access',
      accessTokenExpiresAt: '2030-01-01T00:00:00Z',
    });

    const retry = httpTesting.expectOne('/api/identity/users');
    expect(retry.request.headers.get('Authorization')).toBe('Bearer next-access');
    retry.flush({ items: [], totalCount: 0, page: 1, pageSize: 20 });
    await pending;

    expect(tokens.getAccessToken()).toBe('next-access');
    expect(onUnauthorized).not.toHaveBeenCalled();
  });

  it('shares one refresh across concurrent 401s', async () => {
    tokens.setAccessToken({
      accessToken: 'expired',
      accessTokenExpiresAt: '2020-01-01T00:00:00Z',
    });

    const firstPending = firstValueFrom(http.get('/api/identity/users'));
    const secondPending = firstValueFrom(http.get('/api/authorization/permissions'));

    const first = httpTesting.expectOne('/api/identity/users');
    const second = httpTesting.expectOne('/api/authorization/permissions');
    first.flush(null, { status: 401, statusText: 'Unauthorized' });
    second.flush(null, { status: 401, statusText: 'Unauthorized' });

    const refresh = httpTesting.expectOne('/api/identity/refresh');
    refresh.flush({
      accessToken: 'shared-access',
      accessTokenExpiresAt: '2030-01-01T00:00:00Z',
    });

    const retryUsers = httpTesting.expectOne('/api/identity/users');
    const retryPermissions = httpTesting.expectOne('/api/authorization/permissions');
    retryUsers.flush({ items: [], totalCount: 0, page: 1, pageSize: 20 });
    retryPermissions.flush({ items: [], totalCount: 0, page: 1, pageSize: 20 });

    await Promise.all([firstPending, secondPending]);
    expect(tokens.getAccessToken()).toBe('shared-access');
    expect(onUnauthorized).not.toHaveBeenCalled();
  });

  it('clears the session and invalidates when refresh fails', async () => {
    tokens.setAccessToken({
      accessToken: 'expired',
      accessTokenExpiresAt: '2020-01-01T00:00:00Z',
    });

    const pending = firstValueFrom(http.get('/api/identity/users')).catch(
      (error: unknown) => error,
    );
    const first = httpTesting.expectOne('/api/identity/users');
    first.flush(null, { status: 401, statusText: 'Unauthorized' });

    const refresh = httpTesting.expectOne('/api/identity/refresh');
    refresh.flush(null, { status: 401, statusText: 'Unauthorized' });

    const error = await pending;
    expect(error).toBeInstanceOf(HttpErrorResponse);
    expect(tokens.getAccessToken()).toBeNull();
    expect(onUnauthorized).toHaveBeenCalledTimes(1);
  });
});
