# @fee/api-client

Generated TypeScript client for the Fee Platform API.

## Rule (frozen)

**Do not hand-write request/response shapes.** All HTTP types come from the
backend OpenAPI document (NSwag / Swashbuckle), matching `CONTRACT.md`.

## Generate (planned)

```bash
# from repo root, after the API is running and OpenAPI is published:
pnpm --filter @fee/api-client generate
```

Expected pipeline (P0 stub → P1):

1. Host exports `openapi/v1.json` (or `/swagger/v1/swagger.json`).
2. Generator (e.g. `openapi-typescript` or NSwag) writes into `src/generated/`.
3. Apps import only from `@fee/api-client`.

## Status

P0: placeholder only. Calling `assertGeneratedClient()` throws until generation exists.

## Align with CONTRACT

When Agent S merges `CONTRACT.md`, regenerate and confirm error payload + enums
match `packages/validation`.
