# Fee Platform (A2Z Creatorz)

Multi-tenant fee calculation + parent payment platform. See `FEE_PLATFORM_IMPLEMENTATION_PLAN.md` and `DESIGN_SUPERVISOR.md`.

## P0 layout

```
apps/
  parent-web/           # Next.js shell (Agent F owns features)
  institution-portal/   # Next.js shell (Agent E owns features)
  platform-admin/       # Next.js shell
packages/
  ui/                   # Design tokens + Button / StatusPill (Agent A)
  money/                # PKR format/parse
  validation/           # zod schemas + provisional enums
  api-client/           # OpenAPI-generated types (committed)
backend/
  FeePlatform.slnx      # Modular monolith host
  migrations/           # SQL / RLS stubs (no Migrate-at-startup)
docs/supervisor/        # Requests to Agent S
```

## Prerequisites

- Node 22+, pnpm 9
- .NET 10 SDK
- **Optional for local API / CI-green work:** PostgreSQL 16+ and Redis
  - Unit/integration tests use EF Core **InMemory** (no Docker, no local Postgres, no Redis required).
  - Hangfire and Redis are config-gated (`Hangfire:Enabled`, `Redis:Enabled`).

## Quick start

```powershell
pnpm install
pnpm run build

dotnet build backend/FeePlatform.slnx
dotnet test backend/FeePlatform.slnx
```

Copy `.env.example` to `.env` for local values. Never commit secrets.

## OpenAPI → `@fee/api-client` (CONTRACT section 9.1)

```powershell
# 1) Refresh openapi.json from the API (InMemory test host — no Postgres)
pwsh ./scripts/export-openapi.ps1

# 2) Generate committed TypeScript types
pnpm --filter @fee/api-client generate
```

CI regenerates from the committed `openapi.json` and fails if `src/generated/` drifts.

## Auth (dev / test)

| Path | Status |
|---|---|
| `POST /api/v1/auth/otp/challenge` + `/otp/verify` | Works in tests with InMemory store; challenge returns `devCode` in Development/Testing |
| `POST /api/v1/auth/portal/login` | Password hash verify (ASP.NET Identity hasher); §3 errors on failure |
| Portal TOTP | Still stubbed (`501 TOTP_STUB`) unless `Auth:AllowTotpBypass=true` in Development |

### Still needs staging Postgres (plan section 17 “login on staging”)

- Real PostgreSQL + RLS-backed isolation test (query-filter tests cover EF filters only today)
- Redis-backed OTP rate limits (in-memory limiter is for tests/dev)
- Hangfire outbox dispatch against Postgres storage
- Production TOTP verification (not bypass)
- Seeded portal users + SMS delivery for parent OTP on staging

## CONTRACT.md

Owned by Agent S. Agents must not edit it. Align packages and APIs to the published contract.
