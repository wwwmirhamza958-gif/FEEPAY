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
  api-client/           # OpenAPI-generated client placeholder
backend/
  FeePlatform.sln       # Modular monolith host
  migrations/           # SQL / RLS stubs (no Migrate-at-startup)
docs/supervisor/        # Requests to Agent S
```

## Prerequisites

- Node 22+, pnpm 9
- .NET 10 SDK
- PostgreSQL 16+ and Redis (for full API runtime; build does not require them)

## Quick start

```powershell
pnpm install
pnpm run build

dotnet build backend/FeePlatform.slnx
dotnet test backend/FeePlatform.slnx
```

Copy `.env.example` to `.env` for local values. Never commit secrets.

## CONTRACT.md

Owned by Agent S. If missing, packages use provisional shapes with `TODO(align-CONTRACT)`.
