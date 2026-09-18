# Plantilla .NET + React + TypeScript

A production-grade starter for building a full-stack web application with **.NET 8 (API)** and **React 19 + TypeScript (SPA)**. The repository ships with hardened authentication, a generic ownership-aware CRUD stack, end-to-end Zod validation, automated test + lint gates, and a curated security baseline derived from the OWASP Top 10.

---

## Stack

### Backend — `Project.Server/`

| Concern | Choice |
|---|---|
| Runtime | .NET 8 (`net8.0`) |
| Auth | JWT bearer (HS256) with `ValidateIssuer`, `ValidateAudience`, `ClockSkew=30s` |
| Sessions | HttpOnly + `SameSite=Strict` refresh cookies, rotated on every refresh, reuse-detection revokes the whole chain |
| CSRF | Double-submit (`XSRF-TOKEN` cookie ↔ `X-XSRF-TOKEN` header) on the refresh endpoint |
| ORM | EF Core 8 with SQL Server / PostgreSQL / MySQL providers |
| Validation | FluentValidation 12 with keyed validators (`Create` / `Update` / `Partial`) |
| Mapping | Mapster 7 (self-maps for each entity) |
| Lockout | 5 failed attempts / 30 min, persistent via `FailedLoginAttempts` + `LockoutEnd` |
| Password policy | Shared `SecurityPasswordPolicy` (8+ chars, upper/lower/digit/special) |
| Recovery tokens | `RandomNumberGenerator.GetBytes(32)` → Base64Url; only the SHA-256 hash is persisted |
| Generic CRUD | `IEntityService<T,TReq,TId>` with 18 async methods, `IOwnedEntity<TId>`-aware, RBAC-fail-closed |
| Query safety | `IQueryPolicy<TEntity>` allowlist — `Password`, `RecoveryToken`, `DateToken`, `FailedLoginAttempts`, `LockoutEnd`, `OwnerId` are unreachable from the URL |
| Soft delete | Global `HasQueryFilter(State != 0)` on every `IEntity<T>`; `Restore` via `IgnoreQueryFilters()` |
| Rate limiting | Global 60/min/IP + `auth` policy 5/15 min/IP for login / recovery / refresh / token-validation |
| Security headers | CSP, `nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`, restrictive Permissions-Policy, COOP/CORP |
| Errors | `UseExceptionHandler` writes ProblemDetails with `traceId` — no `ex.Message` leaks |
| Logging | Serilog (console sink + structured logging; `OperationAuthorizationHandler` no longer dumps the full permissions list) |
| Audit | `LoginAudit` + `PasswordHistory` rows written for every login + password change |

### Frontend — `project.client/`

| Concern | Choice |
|---|---|
| Runtime | React 19.2 + Vite 6.3 + TypeScript 5.8 |
| UI | HeroUI 3 + Tailwind CSS 4 (no `unsafe-inline` script in CSP) |
| State | Zustand 5 (auth in module-scoped memory, never `localStorage`) |
| Data fetching | TanStack Query 5 |
| HTTP | Axios 1 with a typed interceptor (NetworkError / UnauthorizedError / ForbiddenError / InternalServerError) |
| Validation | Zod 4 — schemas in `src/types/schemas.ts`, validated through `src/services/zodApi.ts` |
| Routing | React Router 7 with `ProtectedRoute` (auth → redirect → permission order) + `useCan(operationKey)` |
| Persistence | Refresh token in HttpOnly cookie; SPA reads `XSRF-TOKEN` for the CSRF header |
| Lint | ESLint 9 with curated config (`--max-warnings=0`); `jsx-a11y`, `unused-imports`, `perfectionist`, `@tanstack/eslint-plugin-query` all activated |
| Format | Prettier 3 |
| Tests | Vitest 5 + `@testing-library/react` (jsdom) — 24 tests across 5 files |
| Hooks | Husky 9 + lint-staged 17 — pre-commit runs `lint-staged`, pre-push runs `npm run verify` |

---

## Project layout

```
Project.Server/
├── Configs/              # ServiceCollection extensions (JWT, CORS, headers, rate-limit, Mapster, validators)
├── Context/              # DbContext + EF configurations
├── Controllers/          # HTTP endpoints (AuthController, CrudController, 4 derived CRUD)
├── Entities/             # Domain entities, request/response DTOs, interfaces (IEntity, IOwnedEntity)
├── Interceptors/         # Before/After Delete, Before/Update/Partial Create/Update, IEntityQueryFilter
├── Mappers/              # MapsterConfig.RegisterMappings()
├── Migrations/           # EF Core migrations (incl. AddRefreshTokens)
├── Security/             # Authorization handler + OperationRequirement
├── Services/             # AuthService, EntityService, EntitySupportService, QueryPolicy, etc.
├── Utils/                # OwnershipResolver, SortTranslator, Util (MassAssignmentDenylist)
├── Validations/          # FluentValidation validators per entity
└── appsettings*.json     # Empty connection strings; secrets come from user-secrets / env vars

project.client/
├── src/
│   ├── components/       # UI building blocks
│   ├── configs/          # axios interceptors, constants (route names, no API URL)
│   ├── containers/       # Layout shell
│   ├── hooks/            # useAuth, useCan, useAuthorizationRoutes, useForm, useResponse
│   ├── pages/            # Route-level views
│   ├── routes/           # PublicRoutes + middleware (ProtectedRoute, ProtectedPublic, ProtectedError)
│   ├── services/         # zodApi, userService, rolService, operationService, ...
│   ├── stores/           # useAuthStore (in-memory token + refresh bootstrap)
│   ├── styles/           # Tailwind theme
│   ├── test/             # vitest setup
│   ├── types/            # DTOs + zod schemas
│   └── utils/            # Helpers
├── .env.example          # VITE_API_URL stub
├── eslint.config.js      # Curated, --max-warnings=0
├── index.html            # CSP <meta> fallback
├── package.json          # verify / typecheck / lint / format / test / lint:fix
└── vitest.config.ts      # jsdom + tsconfig-paths

.github/workflows/ci.yml # Backend build + test, frontend verify, audit (--audit-level=high)
agents.md                # Project conventions (frontend strict rules + backend best practices + OWASP control matrix)
plans/                   # Historical execution plans
```

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download)
- [Node.js 20+](https://nodejs.org) (the CI uses Node 20)
- A SQL Server, PostgreSQL, or MySQL instance — connection strings are seeded empty; supply via user-secrets or environment variables
- `dotnet user-secrets` enabled on `Project.Server`

---

## First-time setup

### Backend

```bash
cd Project.Server
dotnet restore

# Provide the minimum secrets via user-secrets (Development) or env vars (everything else).
dotnet user-secrets init   # already wired through <UserSecretsId> in the csproj

# Required: the JWT signing key. Must be ≥ 32 bytes UTF-8.
dotnet user-secrets set "AppSettings:Secret" "REPLACE-WITH-AT-LEAST-32-RANDOM-BYTES"

# Required: the SA seed password. Used by AdminSeedHostedService on first run.
dotnet user-secrets set "AppSettings:SeedAdminPassword" "Choose-A-Strong-Password"

# Database connection (example — pick your provider in appsettings.*.json):
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=ProyectoDb;User=root;Password=root;"
```

`AdminSeedHostedService` refuses to start outside Development when `SeedAdminPassword` is missing.

### Frontend

```bash
cd project.client
npm install
cp .env.example .env.local   # set VITE_API_URL only if your backend is not on the same origin
```

---

## Development

Run both processes — Vite proxies API calls to the .NET backend:

```bash
# Terminal 1
dotnet run --project Project.Server

# Terminal 2
cd project.client
npm run dev
```

Other useful scripts:

```bash
# Backend
dotnet build Project.sln --configuration Release -warnaserror
dotnet test  Project.sln --configuration Release
dotnet ef migrations list --project Project.Server

# Frontend
cd project.client
npm run typecheck
npm run lint       # ESLint with --max-warnings=0
npm run format     # Prettier write
npm run format:check
npm run test       # vitest run
npm run verify     # typecheck + lint + format:check + test (used in pre-push hook + CI)
```

---

## Configuration

### `appsettings.json` (committed, non-secret)

```jsonc
{
  "AppSettings": {
    "Issuer": "Plantilla",
    "Audience": "Plantilla.Client",
    "TokenExpirationHrs": 1,
    "NotBefore": 0,
    "CorsAllowedOrigins": [ "http://localhost:5173" ],
    "RegisterRoleId": 2
  },
  "Pagination": {
    "DefaultPageSize": 30,
    "MaxPageSize": 100,
    "MinPageNumber": 1
  }
}
```

Add a per-provider file (`appsettings.MySql.json`, `appsettings.PostgreSql.json`, `appsettings.SqlServer.json`) with `Database.ConnectionString` + `ConnectionStrings.DefaultConnection`. The committed copies are empty placeholders — your real values come from user-secrets or environment variables (`AppSettings__Secret`, `AppSettings__SeedAdminPassword`, `ConnectionStrings__DefaultConnection`, etc.).

### `.env.local` (frontend, never committed)

```bash
# Base URL of the backend. Empty means same-origin (Vite dev proxy).
# Anything prefixed VITE_ is exposed to the browser bundle — never put secrets here.
VITE_API_URL=
```

---

## Architecture highlights

### Ownership-aware CRUD

Every entity that carries user data implements `IOwnedEntity<TId>` (or `IEntity<TId>` — the resolver falls back to `CreatedBy`). The `OwnershipValidationHostedService` refuses to start if a registered entity implements neither contract.

`CrudController<TEntity, TRequest, TResponse, TId>` exposes:

| Verb | Route | Purpose |
|---|---|---|
| `GET` | `?` | Admin list (requires operation) |
| `GET` | `count` | Count |
| `GET` | `{id}` | Admin get by id |
| `POST` | `?` | Admin create |
| `PUT` | `?` | Admin update |
| `PATCH` | `?` | Admin partial update |
| `DELETE` | `{id}` | Admin delete |
| `POST` | `{id}/restore` | Recover a soft-deleted row |
| `GET` | `me` | List the current user's rows |
| `GET` | `me/count` | Count the current user's rows |
| `GET` | `me/{id}` | Get one of the current user's rows; **403** if it exists but belongs to someone else |
| `POST` | `me` | Create with the owner forced from the token |
| `PUT` | `me` | Update one of the current user's rows |
| `PATCH` | `me` | Partial update one of the current user's rows |
| `DELETE` | `me/{id}` | Delete one of the current user's rows |

Every action carries `[RequireOperation]`. A startup convention (`RequireOperationConventionProvider`) walks the action model and **refuses to start** if any action is exposed without `[RequireOperation]` or `[AllowAnonymous]`.

### Query safety

`IQueryPolicy<TEntity>` enforces the allowlist at the service boundary. Sensitive fields — `Password`, `PasswordHash`, `RecoveryToken`, `DateToken`, `FailedLoginAttempts`, `LockoutEnd`, `MustChangePassword`, `OwnerId` — are not filterable, sortable, or includable. `Include` paths are capped at depth 2 and 3 paths per request.

### Mass assignment

`Util.MassAssignmentDenylist` (in `Util.cs`) blocks `Id`, `CreatedBy`, `CreatedAt`, `UpdatedAt`, `UpdatedBy`, `State`, `Password`, `PasswordHash`, `RecoveryToken`, `DateToken`, `FailedLoginAttempts`, `LockoutEnd`, `MustChangePassword`, `OwnerId`, `RolId`, `UserId`. A `PUT /User` body with `rolId: 1` cannot escalate the user.

### Authentication flow

1. `POST /Auth` → `bcrypt.Verify(password, user.Password)` → `LockoutEnd`/`FailedLoginAttempts` → issues an access token (15 min) and sets the refresh cookie + XSRF cookie.
2. `POST /Auth/Refresh` → requires `XSRF-TOKEN` cookie + `X-XSRF-TOKEN` header to match. Rotates the refresh cookie, issues a new access token. Reuse of a rotated token revokes the entire chain for that user.
3. `POST /Auth/Logout` → revokes the current refresh cookie. Idempotent.

The SPA's `useAuthStore.syncAuth` calls `POST /Auth/Refresh` on cold load. If a valid refresh cookie exists, the access token rehydrates the store without an interactive login.

### Rate limiting

| Policy | Limit | Applied to |
|---|---|---|
| `global` | 60 req / min / IP | every endpoint |
| `auth` | 5 req / 15 min / IP | `Login`, `RecoveryPassword`, `Refresh`, `ValidateToken` |

### Zod-validated API responses

The client never trusts the wire format. `services/zodApi.ts` parses every response through the envelope schema + a per-entity schema and raises a typed `SchemaError` on mismatch. Service public types stay compatible with the existing `ApiResponse<T>` discriminated union.

### Frontend security

- The access token lives in a module-scoped variable inside `interceptors.ts` — never `localStorage`.
- `ProtectedRoute` checks auth → redirect → permission, in that order.
- `useCan(operationKey)` for buttons and inline actions.
- No stacks or `ex.Message` in user-facing UI (`useForm`).
- A `<meta http-equiv="Content-Security-Policy">` in `index.html` mirrors the backend header for `vite preview`.
- All environment variables prefixed `VITE_*` are exposed to the bundle; `.env*` is git-ignored.

---

## Testing

- Backend: xUnit + EF Core InMemory + `Microsoft.AspNetCore.Mvc.Testing`. Run with `dotnet test`.
- Frontend: Vitest + `@testing-library/react` (jsdom). 24 tests covering the auth interceptor, zodApi envelope validation, `useCan`, `ProtectedRoute`, `ProtectedPublic`. Run with `npm run test`.

CI runs both gates on every push and pull request to `master`:

- `dotnet build -warnaserror` + `dotnet test` (Release)
- `npm ci` + `npm run typecheck` + `npm run lint` + `npm run build` + `npm audit --audit-level=high`

---

## Conventions

The project's mandatory conventions — including the strict frontend rules (0 warnings, no `eslint-disable`, no `any`, no secrets in `localStorage`, every action gated, every route guarded, every action `useCan`-checked, every API response validated, no secrets in `VITE_*`) and the backend best practices (build with `-warnaserror`, no committed secrets, every controller action `[RequireOperation]`, every user-data entity `IOwnedEntity<long>` + `me/...` endpoints, every query through `IQueryPolicy`) — live in `agents.md`. Read it before opening a PR.

OWASP Top-10 control matrix with file paths for each category is also in `agents.md`.

---

## License

This template is provided under the same license as the parent repository. Adapt as needed for your downstream project.
