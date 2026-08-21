import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';

import { APP_CONFIG } from '../../../core/config/app-config';
import { AuthorizationApi } from './authorization.api';

describe('AuthorizationApi', () => {
  let api: AuthorizationApi;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
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

    api = TestBed.inject(AuthorizationApi);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('maps permission page results', async () => {
    const pending = firstValueFrom(api.listPermissions({ page: 2, pageSize: 10 }));
    const request = httpTesting.expectOne(
      (req) =>
        req.url === '/api/authorization/permissions' &&
        req.params.get('page') === '2' &&
        req.params.get('pageSize') === '10',
    );

    request.flush({
      items: [
        {
          id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
          code: 'users.read',
          name: 'Read users',
          module: 'users',
          action: 'read',
          isActive: true,
          createdAt: '2030-01-01T00:00:00Z',
        },
      ],
      totalCount: 1,
      page: 2,
      pageSize: 10,
    });

    const page = await pending;
    expect(page.items[0]?.name).toBe('users.read');
    expect(page.items[0]?.displayName).toBe('Read users');
    expect(page.totalCount).toBe(1);
  });
});
