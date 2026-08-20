# Client

Angular 21 workspace client for the .NET API.

## Prerequisites

- Node.js 22+
- npm 11+

## Commands

```bash
npm start          # ng serve at http://localhost:4200
npm run build      # production build
npm test           # Vitest unit tests
npm run lint       # ESLint
npm run format     # Prettier write
npm run format:check
npm run e2e        # Playwright end-to-end tests
```

## Architecture

See [ARCHITECTURE.md](./ARCHITECTURE.md) for feature boundaries, auth ports, NgRx conventions, and how to wire the backend later.

## Local auth demo

The mock auth adapter persists a demo session in `sessionStorage` so refresh keeps you signed in during local development. When the backend is ready, swap the `AUTH_PORT` provider to `IdentityHttpAdapter`.

Demo account behavior:

- any valid email/password with `@` signs in as an admin-equivalent user

## E2E

Playwright starts the dev server automatically via `playwright.config.ts`.

```bash
npm run e2e
```
