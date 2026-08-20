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
- Facades expose selectors/actions to components; pages should not dispatch raw store actions
- Do not add Entity/Effects for a feature that only needs a simple list load unless pagination, caching, or shared consumers justify it
- Components stay OnPush and read auth/UI through ports or facades

## Auth foundation

Auth is modeled for **HttpOnly cookie sessions**:

- `AuthPort` defines `login`, `register`, `logout`, `restoreSession`
- `IdentityMockAdapter` simulates cookie persistence with `sessionStorage` during local development
- `IdentityHttpAdapter` is ready for backend wiring with `withCredentials: true`
- NgRx identity state stores **CurrentUser only**, never access/refresh tokens
- `AUTH_STATE` and `AUTH_COMMANDS` let `core`/`layout` consume auth without importing identity internals

Bootstrap flow:

1. `App` dispatches `IdentityActions.appStarted()`
2. effect calls `AuthPort.restoreSession()`
3. guards wait until status is no longer `initializing`

## HTTP conventions

- `mapHttpError()` converts Problem Details to user-facing messages
- `correlationIdInterceptor` adds `X-Correlation-Id`
- `UNAUTHORIZED_HANDLER` clears session on `401`
- list endpoints should eventually return `PageResult<T>`

## UI conventions

- Pages use `app-page-header` + `app-request-state`
- Async errors use `role="alert"` / `aria-live`
- Loading indicators expose an accessible label
- Empty/error/retry behavior must be consistent across list pages

## Adding a new feature

1. Create `features/<name>/` with `pages/`, `data-access/`, `state/`, `models/`, `<name>.routes.ts`, `index.ts`
2. Lazy-load the route from `app.routes.ts`
3. Define API contracts in `data-access/` and map errors through `mapHttpError`
4. Expose a facade for components
5. Add reducer/effect tests and at least one route or component test

## Switching auth from mock to backend

Development defaults to `useMockAuth: true` in `environment.development.ts`.
Production defaults to `useMockAuth: false` and binds `IdentityHttpAdapter`.

To force HTTP auth in local development:

1. Set `useMockAuth: false` in `environment.development.ts`
2. Ensure the API is reachable at `apiBaseUrl` and sets HttpOnly cookies for:
   - `POST /auth/login`
   - `POST /auth/register`
   - `POST /auth/logout`
   - `GET /auth/me`

`provideIdentityAuth()` selects the adapter from `APP_CONFIG.useMockAuth`.
No component or guard changes are required if the adapter honors `AuthPort`.
