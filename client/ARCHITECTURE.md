# Frontend Architecture

This Angular 21 client is organized as a training-ready base. Business features stay isolated, shared contracts live in `core`, and backend integration happens through ports/adapters.

## Layering

```text
src/app/
  core/           # singleton infrastructure, auth/http contracts, guards
  shared/         # reusable UI and utilities only
  layout/         # application shell
  features/       # vertical slices (identity, users, authorization, ...)
```

Dependency direction:

- `features` may depend on `core` and `shared`
- `layout` may depend on `core` and `shared`
- `core` must not depend on feature implementations
- features must not deep-import other features; use public `index.ts` exports when needed
- shared auth contracts (`SystemPermissions`, `AUTH_STATE`, `AUTH_COMMANDS`) live in `core`

## State management

Use the lightest tool that fits:

| Concern | Prefer | Avoid |
| --- | --- | --- |
| Local UI (sidenav open, form draft, tab index) | Signals / `linkedSignal` | NgRx |
| Derived view state from inputs/store | `computed` | Effects that write back into state |
| Session / shared server state / multi-route workflows | NgRx feature store + effects | Ad-hoc services with mutable fields |
| One-page fetch with no reuse | `resource` / `httpResource` or a thin facade | Full actions/reducers/effects |

Rules:

- Feature state is route-scoped when possible (`provideState` on feature routes)
- Identity session state is registered by `provideIdentityAuth()`, not by `core`
- Facades expose selectors/actions to components; pages should not dispatch raw store actions
- List features use shared `ListState<T>` / `PagedQueryState` helpers from `core/store/list-state`
- List load effects use `switchMap` so newer page requests cancel stale in-flight ones
- Components stay OnPush and read auth/UI through ports or facades

## Auth foundation

Auth uses **Bearer access tokens + HttpOnly refresh cookies**:

- `AuthPort` defines `login`, `register`, `logout`, `restoreSession`
- `AuthCommandsPort.bootstrap()` starts session restore from `App` (no feature action imports in `app.ts`)
- `IdentityMockAdapter` simulates local sessions for UI-only work
- `IdentityHttpAdapter` calls `/api/identity/*` with `withCredentials: true`
- Access tokens live in `TokenSession` memory only and are never written to NgRx
- Refresh tokens are set by the API as `HttpOnly` cookies (`refresh_token`, path `/api/identity`)
- `AUTH_STATE` and `AUTH_COMMANDS` let `core`/`layout` consume auth without importing identity internals

Bootstrap flow:

1. `App` calls `AUTH_COMMANDS.bootstrap()`
2. effect calls `AuthPort.restoreSession()`
3. restore tries `/identity/me` when an access token exists, otherwise `/identity/refresh` via cookie
4. guards wait until status is no longer `initializing`

HTTP interceptor order (outer → inner):

1. `correlationIdInterceptor`
2. `unauthorizedInterceptor` (hard 401 after refresh skipped/failed retry)
3. `authInterceptor` (attach Bearer + shared single-flight refresh on 401)

Local development:

1. API runs on `http://localhost:5050`
2. Angular uses `apiBaseUrl: '/api'` and `proxy.conf.json` so browser traffic is same-origin on port `4200`
3. Set `useMockAuth: false` in `environment.development.ts` to use the real API

Identity endpoints:

- `POST /api/identity/login`
- `POST /api/identity/register`
- `POST /api/identity/refresh`
- `POST /api/identity/revoke`
- `GET /api/identity/me`
- `GET /api/identity/users`

## HTTP conventions

- `mapHttpError()` converts Problem Details to user-facing messages
- `correlationIdInterceptor` adds `X-Correlation-Id`
- `authInterceptor` attaches Bearer tokens and performs a single shared refresh on `401`
- Refresh failure (or hard retry `401`) calls `UNAUTHORIZED_HANDLER` to clear session
- list endpoints return `PageResult<T>` and use shared `app-page-pager`

## UI conventions

- Pages use `app-page-header` + `app-request-state`
- Async errors use `role="alert"` / `aria-live`
- Loading indicators expose an accessible label
- Empty/error/retry behavior must be consistent across list pages

## Adding a new feature

1. Create `features/<name>/` with `pages/`, `data-access/`, `state/`, `models/`, `<name>.routes.ts`, `index.ts`
2. Lazy-load the route from `app.routes.ts`
3. Define API contracts/mappers in `data-access/` and map errors through `mapHttpError`
4. Prefer `ListState<T>` + facade for paged lists; register state with `provideState` on the feature route
5. Use `switchMap` in list effects
6. Add reducer/effect/data-access tests and at least one route or component test
