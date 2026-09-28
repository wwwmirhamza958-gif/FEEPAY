# Payments module (Agent D — P3)

Owns payment adapters, attempts, receipts, pay-links, offline recording, and reconciliation stubs.

## Packages

| Package | Version | Use |
|---|---|---|
| QuestPDF | 2026.9.1 | Receipt PDFs (Community license set in code) |
| QRCoder | 1.8.0 | Receipt verification QR |

No unofficial Raast / JazzCash / Easypaisa / card-entry SDKs. Adapters call provider HTTP APIs when credentials exist.

## FakeTier1 (sandbox without bank credentials)

When **both** are true:

1. `Payments:AllowSandboxFake=true`
2. Host environment is **Development**

Tier-1 adapters (`raast-p2m-pilot`, `jazzcash-pilot`, `easypaisa-pilot`) return a fake checkout URL and accept sandbox callbacks / `POST /api/v1/payments/sandbox/fake-complete/{attemptId}`.

**Production must keep `AllowSandboxFake=false`.** Without credentials and without FakeTier1, adapters return `PAYMENTS_SANDBOX_NOT_CONFIGURED` (never a fake success).

## Configuration

```json
"Payments": {
  "AllowSandboxFake": false,
  "PublicBaseUrl": "https://api.example",
  "PayLinkSigningKey": "<secret>",
  "Adapters": {
    "raast-p2m-pilot": {
      "Enabled": true,
      "SecretRef": "tenant/{tenantId}/adapters/raast-p2m-pilot",
      "CallbackHmacSecret": "<from secret store>",
      "MerchantId": "<from secret store>",
      "ApiBaseUrl": "https://bank-sandbox.example"
    }
  }
}
```

Secrets stay in configuration / secret refs — never hard-coded and never stored on `TenantAdapterConfig` rows (only `secret_ref`).

## Endpoints

| Method | Path | Notes |
|---|---|---|
| GET | `/api/v1/payments/channels?tenantId=` | Highest enabled tier as default + others (not flattened) |
| POST | `/api/v1/payments/attempts` | Create / resume; idempotency key |
| GET | `/api/v1/payments/attempts/{id}` | Status (return URL is not proof) |
| POST | `/api/v1/payments/callbacks/{adapterCode}` | Signature verified; invalid → reject |
| GET | `/api/v1/payments/receipts/{id}` | Metadata (`?pdf=true` regenerates PDF) |
| GET | `/api/v1/payments/receipts/{id}/pdf` | PDF bytes |
| GET | `/api/v1/payments/receipts/verify/{token}` | Public verify (validity, amount, date, initials) |
| POST | `/api/v1/payments/pay-links` | Signed expiring link for one invoice |
| POST | `/api/v1/payments/offline` | Finance officer manual payment (reference required) |
| POST | `/api/v1/payments/reconciliation/runs` | Stub: three exception types |

Tenant via `X-Tenant-Id`. Errors use CONTRACT §3 `ApiErrors` envelope.

## Receipts

Gap-free per-tenant receipt numbers. PDF includes institution name, lines, receipt number, channel, gateway ref, QR — **no tax line**.
