import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';

import { APP_CONFIG } from '../../../core/config/app-config';
import type { PageQuery, PageResult } from '../../../core/http/page-result.model';
import type {
  OrganizationUnit,
  PermissionDefinition,
  UserGroup,
} from '../models/authorization.models';
import {
  mapOrganizationUnit,
  mapPageResult,
  mapPermissionDefinition,
  mapUserGroup,
  type OrganizationUnitDto,
  type PermissionDefinitionDto,
  type UserGroupDto,
} from './authorization.contracts';

const DEFAULT_PAGE_SIZE = 20;

@Injectable({ providedIn: 'root' })
export class AuthorizationApi {
  private readonly http = inject(HttpClient);
  private readonly config = inject(APP_CONFIG);

  private get baseUrl(): string {
    return `${this.config.apiBaseUrl}/authorization`;
  }

  listPermissions(query?: Partial<PageQuery>): Observable<PageResult<PermissionDefinition>> {
    return this.http
      .get<PageResult<PermissionDefinitionDto>>(`${this.baseUrl}/permissions`, {
        params: toParams(query),
        withCredentials: true,
      })
      .pipe(map((page) => mapPageResult(page, mapPermissionDefinition)));
  }

  listGroups(query?: Partial<PageQuery>): Observable<PageResult<UserGroup>> {
    return this.http
      .get<PageResult<UserGroupDto>>(`${this.baseUrl}/groups`, {
        params: toParams(query),
        withCredentials: true,
      })
      .pipe(map((page) => mapPageResult(page, mapUserGroup)));
  }

  listOrganizationUnits(
    query?: Partial<PageQuery>,
  ): Observable<PageResult<OrganizationUnit>> {
    return this.http
      .get<PageResult<OrganizationUnitDto>>(`${this.baseUrl}/organization-units`, {
        params: toParams(query),
        withCredentials: true,
      })
      .pipe(map((page) => mapPageResult(page, mapOrganizationUnit)));
  }
}

function toParams(query?: Partial<PageQuery>): HttpParams {
  let params = new HttpParams()
    .set('page', String(query?.page ?? 1))
    .set('pageSize', String(query?.pageSize ?? DEFAULT_PAGE_SIZE));

  if (query?.search) {
    params = params.set('search', query.search);
  }

  if (query?.sort) {
    params = params.set('sort', query.sort);
  }

  return params;
}
