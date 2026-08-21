import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';

import { APP_CONFIG } from '../../../core/config/app-config';
import type { PageQuery, PageResult } from '../../../core/http/page-result.model';
import type { UserSummary } from '../models/user.models';

interface UserResponseDto {
  id: string;
  email: string;
  groups: readonly string[];
}

@Injectable({ providedIn: 'root' })
export class UsersApi {
  private readonly http = inject(HttpClient);
  private readonly config = inject(APP_CONFIG);

  list(query?: Partial<PageQuery>): Observable<PageResult<UserSummary>> {
    let params = new HttpParams()
      .set('page', String(query?.page ?? 1))
      .set('pageSize', String(query?.pageSize ?? 20));

    if (query?.search) {
      params = params.set('search', query.search);
    }

    return this.http
      .get<PageResult<UserResponseDto>>(`${this.config.apiBaseUrl}/identity/users`, {
        params,
        withCredentials: true,
      })
      .pipe(
        map((page) => ({
          items: page.items.map(
            (item): UserSummary => ({
              id: String(item.id),
              email: item.email,
              groups: item.groups,
            }),
          ),
          totalCount: page.totalCount,
          page: page.page,
          pageSize: page.pageSize,
        })),
      );
  }
}
