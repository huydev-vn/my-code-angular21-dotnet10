import { Injectable } from '@angular/core';
import { Observable, delay, of } from 'rxjs';

import type { UserSummary } from '../models/user.models';

const users: readonly UserSummary[] = [
  {
    id: '11111111-1111-1111-1111-111111111111',
    email: 'admin@local.dev',
    groups: ['System Administrators'],
  },
  {
    id: '22222222-2222-2222-2222-222222222222',
    email: 'lead@local.dev',
    groups: ['Leadership'],
  },
  {
    id: '33333333-3333-3333-3333-333333333333',
    email: 'ops@local.dev',
    groups: ['Operations'],
  },
];

@Injectable({ providedIn: 'root' })
export class UsersApi {
  list(): Observable<readonly UserSummary[]> {
    return of(users).pipe(delay(280));
  }
}
