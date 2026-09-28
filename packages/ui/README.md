# @fee/ui

Design system shell for Fee Platform. Implements `DESIGN_SUPERVISOR.md` §3 tokens and §5.1–5.5 / §9 primitives.

## Tokens

Import once in each app:

```ts
import "@fee/ui/tokens.css";
```

Tailwind theme extension:

```ts
import { feeTailwindTheme } from "@fee/ui/tailwind-preset";
```

## Components

- `Button` — primary / secondary / ghost / destructive; press scale `0.97`; primary fill is `--ink`
- `StatusPill` — CONTRACT §2 invoice/payment/clearance kinds (includes Invoice `Draft`); icon + label
- `ImportStatusPill` — CONTRACT §2.4 import job statuses (separate from PaymentAttempt Succeeded/Failed)
- `ConfirmDialog` — centred dialog; optional required reason; portal-dense default; Escape + close

## Rules

- Feature apps never hard-code hex, raw `ms`, `ease-in`, or `transition: all`
- Tenant brand colour is identity only (chip / 3px band), never button fill
- Use logical properties for RTL (`ms`/`me`/`ps`/`pe`)
