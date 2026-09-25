# @fee/api-client

Generated TypeScript types for the Fee Platform API (CONTRACT section 9.1).

## Rule (frozen)

**Do not hand-write request/response shapes.** All HTTP types come from
`openapi.json` via `openapi-typescript`.

## Pipeline

1. **Export OpenAPI** (no Postgres/Redis required — uses EF InMemory test host):

   ```powershell
   pwsh ./scripts/export-openapi.ps1
   ```

   Writes `packages/api-client/openapi.json`.

2. **Generate types**:

   ```powershell
   pnpm --filter @fee/api-client generate
   # or: pwsh ./scripts/generate-api-client.ps1
   ```

   Writes `packages/api-client/src/generated/schema.d.ts` (committed).

3. **CI**: regenerates from the committed `openapi.json` and fails if the working tree differs.

## Usage

```ts
import type { paths, ApiError, ApiErrorEnvelope } from "@fee/api-client";
import { parseApiError } from "@fee/api-client";
```

`parseApiError` is re-exported from `@fee/validation` and always yields a usable
CONTRACT section 3 message (never silent/empty).
