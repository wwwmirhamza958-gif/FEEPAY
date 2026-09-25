# Fee Platform — Design Supervisor Instructions

| | |
|---|---|
| **For** | Every Cursor agent that changes how something **looks, reads, moves or is tapped**: F (Parent web), E (Institution portal), G (Native apps), and A (owner of `packages/ui` and design tokens) |
| **Owned by** | S (Supervisor). Same rule as `CONTRACT.md` (§16.3 of the plan): agents **request** changes, they never edit this file. |
| **Companion to** | `FEE_PLATFORM_IMPLEMENTATION_PLAN.md` v0.2. The plan's **locked decisions (L1–L9)** and **out-of-scope list (§3.2)** override anything here. |
| **Sources** | Emil Kowalski's design-engineering principles (motion, feel, craft) · ui-ux-pro-max dataset (accessibility, touch, typography, palettes, stack rules for Next.js / shadcn / React Native) · the plan's own UX rules (L7, §6.3, §6.6, §9) |
| **Version** | v1.0 · 2026-09-25 |

---

## 0. The one-page card (read this before every UI task)

> **The parent should open the app, understand what they owe within two seconds, and pay with one thumb, without having to think.**
> That is the whole product. Everything below exists to protect it.

1. **One screen, one job, one primary button.** If a screen has two filled buttons, one of them is wrong.
2. **The amount is the headline.** Use the biggest type on the screen for "what I owe", then "by when", then the button that pays it.
3. **Keep the thumb zone for action.** On mobile, the primary action sits in a sticky bottom bar. Never put it at the top-right.
4. **Explain every rupee.** Every line item can be tapped to show its calculation trace, and the parent never sees a number without a reason (§7.6).
5. **The best payment channel goes first.** Show the tenant's highest enabled tier as the default and put everything else under "Other ways to pay" (§6.3). Never show a flat list of four.
6. **Never render our own card field.** Use the provider SDK or a system browser tab only (§6.6). This is a design rule as much as a security rule.
7. **Every state is designed.** That means loading, empty, error, pending, success, offline, RTL and a very long name. A screen with only the happy path is not done.
8. **No silent failure.** Every API error renders a human message plus a way out. The prior A2Z build failed exactly here (§16.1).
9. **Motion should make the product feel faster.** Keep UI transitions under 300 ms, use ease-out, animate only transform and opacity, and honour reduced motion.
10. **Keep the tokens pure.** Every colour, space, radius, shadow, duration and easing comes from `packages/ui` tokens. A raw hex value or `ms` value in a feature file is rejected.

**Where the "wow" comes from:** clarity first, then craft. Parents are impressed when a fee that used to cost them a bank queue takes three taps and ends with a receipt they trust. Delight is saved for the moments that deserve it (§7.4). It is never sprinkled everywhere.

---

## 1. Your role as design supervisor

You are a **design engineer with taste**, not a decorator. In a market where every fee portal is "good enough", the difference is the aggregate of hundreds of details that parents never consciously notice. As Emil Kowalski puts it, the goal is that *a feature works exactly as someone assumes it should, so they proceed without a second thought.*

### 1.1 What the supervisor does

- Owns this file, the token values in the design contract (§3), and the screen blueprints (§6).
- Reviews every UI PR against the **Design Gate** (§11) before merge. The review uses the Before/After table format (§12).
- Settles design disputes between agents. For example, if F and E both need a "status pill", the supervisor defines it once in `packages/ui` and A builds it.
- Keeps the parent web, the portal and the future native apps visibly one family (L8).

### 1.2 What the supervisor does NOT do

- Write feature code. The supervisor reviews and requests (§16.2).
- Add scope. The following never appear in any design: tax lines, BNPL, wallets or balances, a card form, AI/chat widgets, or school-only UI (§3.2).
- Approve "looks cool" motion on anything used frequently (§7.1).

### 1.3 Ownership of design files (maps onto §16.2)

| Path / artefact | Owner | Others |
|---|---|---|
| `DESIGN_SUPERVISOR.md` (this file) | **S** | Read and request only |
| `packages/ui/tokens/*` (colour, type, space, radius, shadow, motion) | **A** implements the values S specifies | F, E and G consume them and never fork them |
| `packages/ui/components/*` (shared primitives) | **A** | F and E request new primitives from S. They do not build look-alikes locally. |
| `apps/parent-web/**` screens | **F** | — |
| `apps/institution-portal/**` screens | **E** | — |
| Native shells (P4) | **G** | Reuse tokens and components. No re-styling. |

**If you need a component that doesn't exist:** stop, open a request to S with the screen, the need and a sketch. Do not build a private `MyButton`. Two lookalike buttons are the design version of the contradictory seed data in §16.1.

---

## 2. Who we are designing for

| Persona | Reality | Design consequence |
|---|---|---|
| **Parent, mid-40s, Lahore** | Budget Android phone, WhatsApp power user, busy, and may prefer Urdu. Anxious about deadlines and admit-card holds. | Large type, high contrast, Urdu parity, one-thumb flows, and deep links from WhatsApp that open straight onto the right invoice. |
| **Parent abroad (Dubai, UK)** | Pays on behalf of the family, in another timezone. | A pay-on-behalf link with no account and no install. Dates are always shown with the day name ("Mon 14 Oct"). |
| **Student paying own fees** | Very fluent on a phone, and impatient. | Speed. Nothing gets in the way. |
| **Relative with a shared link** | Has never seen the app. Suspicious of scams. | The page shows who is asking, for what and how much, plus institution branding and a verification cue. No login. (§9.3) |
| **Finance officer** | At a desk, 8 hours a day, repeats actions hundreds of times. | Dense, fast and keyboard-first. **No decorative motion.** (§9) |

**Network and device assumptions:** mid-range Android (e.g. 4 GB RAM), Chrome, 3G/4G that drops, a 360–412 px wide viewport, and bright outdoor light. **Design at 360 px first.** 375 px is not the smallest phone in Pakistan.

---

## 3. Design contract — tokens

These are the values A implements in `packages/ui/tokens`. **Feature code references tokens only.**

### 3.1 Visual direction

**"Calm, bright, trustworthy. The feel of a modern banking app, but friendlier."**

The base is **Soft-UI-evolved minimalism** (ui-ux-pro-max: `soft-ui-evolution` on a `Minimalism & Swiss` layout):
- warm off-white canvas, white cards, soft two-layer shadows, 12–16 px radii
- generous whitespace, strong type hierarchy, one accent colour
- **not** glassmorphism, **not** dark neon "fintech/crypto", **not** purple AI gradients, **not** neumorphism. The dataset's default fintech palette (dark + gold + purple) was rejected. It reads as crypto, not as a university fee office.

### 3.2 Colour

> **White-label rule (critical).** One parent sees several institutions on one screen (L6). Each tenant has its own brand colour (§3.1 item 1 of the plan). Therefore:
> - **Tenant colour = identity only.** It appears on the institution logo chip, a 3 px band on the institution card and the receipt header.
> - **Product colours = action and meaning.** The Pay button, status colours and links are **identical for every tenant**. A parent must never have to relearn what "Pay" looks like.
> - A tenant colour is never used for text or for a button fill. If a tenant colour fails 3:1 against the surface, it is shown only as a logo chip with a neutral border.

**Light theme (default for parent surfaces):**

| Token | Value | Use | Contrast check |
|---|---|---|---|
| `--canvas` | `#F7F7F4` | App background (warm, not clinical) | — |
| `--surface` | `#FFFFFF` | Cards, sheets | — |
| `--surface-sunken` | `#F1F2EF` | Trace panels, input wells | — |
| `--ink` | `#0F172A` | Primary text, **primary button fill** | 17:1 on surface |
| `--ink-secondary` | `#475569` | Secondary text | 7.5:1 on surface |
| `--ink-tertiary` | `#64748B` | Captions, meta (≥14 px only) | 4.8:1 on surface |
| `--border` | `#E4E6E1` | Card and hairline borders | decorative |
| `--border-strong` | `#CBD0C8` | Input borders | ≥3:1 non-text |
| `--accent` | `#1D4ED8` | Links, selected states, focus ring, progress | 6.7:1 on surface |
| `--accent-soft` | `#EEF2FF` | Selected row or chip background | — |
| `--success` / `--success-soft` | `#15803D` / `#ECFDF3` | Paid, Cleared | 5.0:1 |
| `--warning` / `--warning-soft` | `#B45309` / `#FFF7E6` | Due soon, Pending, Deferment requested | 5.0:1 |
| `--danger` / `--danger-soft` | `#B91C1C` / `#FEF2F2` | Overdue, Not cleared, Failed | 6.5:1 |
| `--on-ink` | `#FFFFFF` | Text on primary buttons | — |

**Why is the primary button ink, not blue?** A near-black "Pay" button (as with Apple Pay and Stripe) looks modern, stays neutral next to any university's brand colour and has the highest possible contrast. Blue is kept for the "interactive, not primary" meaning: links, selection and focus.

**Dark theme:** it is designed alongside light, not inverted. It is required for the portal and optional (follows the system setting) for parent web. Surfaces are `#0B1120` / `#111827` / `#1F2937`, and the status colours shift to their -400 tints. **Every text pair must be re-measured.** Never assume a light-mode pair passes in dark mode.

**Colour is never the only signal.** Every status carries an icon **and** a word (see §5.3).

### 3.3 Typography

| Role | Font | Notes |
|---|---|---|
| Latin UI + headings | **Lexend** (headings) + **Source Sans 3** (body/UI) | From the ui-ux-pro-max "Corporate Trust" pairing. Lexend was designed to improve reading fluency, which suits parents of every literacy level. |
| Money & numbers | **Source Sans 3** with `font-variant-numeric: tabular-nums` | Tabular figures everywhere money appears, so amounts align and never jitter |
| Urdu (RTL) | **Noto Nastaliq Urdu** | Nastaliq is what Urdu readers expect. It needs **line-height ≥ 2.0** and extra vertical padding (`[dir=rtl]` token overrides). Never clip or truncate Nastaliq. |

Load fonts through `next/font` (self-hosted, no layout shift, variable axes). Don't use a Google Fonts `<link>`. Use `font-display: swap`.

**Type scale (mobile), in px:**

| Token | Size / line-height | Weight | Use |
|---|---|---|---|
| `display-amount` | 40 / 44 | 600 | The amount due on Home and Pay |
| `title-1` | 24 / 32 | 600 (Lexend) | Screen title |
| `title-2` | 20 / 28 | 600 (Lexend) | Card title, student name |
| `body` | **16** / 24 | 400 | Everything readable. **Never smaller for body text.** 16 px also prevents iOS input zoom. |
| `body-strong` | 16 / 24 | 600 | Line-item amounts |
| `label` | 14 / 20 | 500 | Pills, captions, meta |
| `micro` | 12 / 16 | 500 | Legal and footnotes only. Never for information a parent needs. |

Respect OS text scaling up to 200%. Layouts must reflow, not truncate. Headings use `text-wrap: balance`.

**Currency format** (implemented once in `packages/money`, never hand-formatted): `Rs 142,500`. Whole rupees, a thin space after "Rs", and Western digits in both English and Urdu (the banking convention in Pakistan). Negative amounts such as discounts are written `−Rs 35,625` with a true minus sign, in `--success`.

### 3.4 Space, radius, elevation, layers

- **Spacing:** 4-pt grid. Tokens are `2, 4, 8, 12, 16, 20, 24, 32, 40, 48, 64`. The mobile screen gutter is `16`, cards are padded `16–20`, and sections are separated by `24–32`. Parent surfaces use the spacious density. The portal uses the standard/dense density (§9).
- **Radius:** `8` inputs/pills · `12` buttons · `16` cards · `24` bottom sheets (top corners) · `full` avatars/chips.
- **Elevation**, soft and two-layered. Only three levels are allowed:
  - `shadow-1` card: `0 1px 2px rgb(15 23 42 / .04), 0 1px 3px rgb(15 23 42 / .06)`
  - `shadow-2` raised/sticky bar: `0 4px 12px rgb(15 23 42 / .08), 0 1px 3px rgb(15 23 42 / .06)`
  - `shadow-3` sheet/dialog: `0 16px 40px rgb(15 23 42 / .16)`
- **Z-index scale:** `0` base · `10` sticky header · `20` sticky action bar · `40` sheet · `50` dialog · `60` toast. Nothing else.

### 3.5 Motion tokens (Emil Kowalski)

```css
--ease-out:     cubic-bezier(0.23, 1, 0.32, 1);    /* default: enter/exit, press, dropdowns */
--ease-in-out:  cubic-bezier(0.77, 0, 0.175, 1);   /* things moving on screen */
--ease-drawer:  cubic-bezier(0.32, 0.72, 0, 1);    /* bottom sheets (iOS-like) */

--dur-press:    140ms;   /* button scale feedback */
--dur-fast:     160ms;   /* tooltips, pills, small popovers */
--dur-base:     220ms;   /* dropdowns, accordions (trace expand), toasts */
--dur-sheet:    320ms;   /* bottom sheets, dialogs */
--dur-exit:     150ms;   /* exits are faster than enters */
```

- **`ease-in` is banned** for UI. It delays the very moment the user is watching.
- **Never `transition: all`.** Name the properties.
- **Animate only `transform` and `opacity`**, plus `clip-path` or `filter: blur` for special cases.
- For the full motion rules, see §7.

### 3.6 Icons

- One set: **Lucide** (it ships with shadcn/ui and has a React Native port for P4). Use a 1.75 px stroke. Sizes are `16 / 20 / 24` only.
- **No emoji as icons.** Ever.
- Decorative icons that sit beside text get `aria-hidden="true"`. Icon-only buttons get an `aria-label`, and they are rare.
- Payment-provider marks (Raast, JazzCash, Easypaisa, card networks) use **official assets** with their clear-space rules. Never recolour them.

---

## 4. Layout and placement: what "easy to access" means here

### 4.1 Mobile thumb-zone map (parent web and native)

```
┌───────────────────────────┐
│  Header: who / where       │  ← read zone: context, never actions that matter
│  (institution, student)    │
├───────────────────────────┤
│                           │
│   THE NUMBER               │  ← eye zone: amount due + due date + status
│   Rs 142,500              │
│   Due Mon 14 Oct · 5 days │
│                           │
│   Supporting content       │  ← scroll zone: line items, instalments, history
│   (cards, lists)           │
│                           │
├───────────────────────────┤
│ ▓▓▓▓▓  Pay Rs 142,500  ▓▓▓ │  ← thumb zone: sticky action bar, one primary button
└───────────────────────────┘     full width, 52 px tall, above the safe-area inset
```

**Placement rules:**
- The **primary action lives in a sticky bottom bar** (`shadow-2`, `z-20`), full width, 52 px tall, padded with `env(safe-area-inset-bottom)`. Content reserves space beneath it so nothing hides behind the bar.
- **The button label says the amount and the verb**: "Pay Rs 71,250", not "Continue" or "Submit".
- **Secondary actions are text or outline buttons,** placed *above* the primary or in an overflow menu. Destructive actions are separated in space and colour.
- **Every touch target is at least 44×44 px** (48 on Android native), with 8 px between targets. Where the visual element is small, extend the hit area.
- **Bottom navigation has at most 4 tabs:** Home · Payments · Requests · Profile. Each tab has an icon **and** a label. No hamburger menu on mobile.
- **Back is predictable.** Browser back must behave, and scroll position and filters are restored. A deep link from WhatsApp that lands mid-flow still gets a sensible back target (Home).

### 4.2 Breakpoints

`360` (design baseline) → `768` → `1024` (the portal switches to a sidebar) → `1440`. On desktop, parent web stays a centred single column with `max-width: 480px`. It is a phone product that happens to open on a laptop. Never allow horizontal scroll. Use `min-h-dvh`, not `100vh`.

### 4.3 RTL (Urdu)

- Use **logical properties only**: `ms-/me-/ps-/pe-`, `start/end`, and `inset-inline`. Never `left/right` or `ml/mr`.
- Directional icons (chevrons, arrows, back) mirror in RTL. Clocks, checkmarks, media controls and logos don't.
- Amounts and dates stay in Western digits and keep their own LTR order inside RTL text (`<bdi>` or `unicode-bidi: isolate`).
- **Every screen is reviewed in both directions** before it passes the Gate. RTL is a first-class layout, not a mirror applied afterwards.

---

## 5. Component rules (built once in `packages/ui`)

Base these on shadcn/ui primitives, restyled to the tokens. Let the primitives handle focus trapping and ARIA. Never hand-roll `focus()` calls.

### 5.1 Buttons

| Variant | Look | When |
|---|---|---|
| `primary` | `--ink` fill, white text, radius 12, height 52 (mobile) / 44 (portal) | One per screen |
| `secondary` | White fill, `--border-strong` border, ink text | Alternatives |
| `ghost` | Text only, `--accent` | Tertiary and inline actions |
| `destructive` | `--danger` text or outline. A fill only in confirm dialogs. | Cancel request, revoke link |

- **Press feedback:** `transform: scale(0.97)` on `:active`, `transition: transform var(--dur-press) var(--ease-out)`. Every pressable element does this, cards included (`0.98`).
- **Loading:** the button keeps its width, its label crossfades (with a 2 px blur during the swap) to a spinner and "Processing…", and it is disabled with `aria-busy`. Never let a double tap create two payment attempts. The API guarantees idempotency (§6.4), and the UI must not invite the double tap in the first place.
- Hover effects are gated behind `@media (hover: hover) and (pointer: fine)`.
- `touch-action: manipulation` on all controls.

### 5.2 Cards

- **Student card (Home):** institution logo chip plus a tenant colour band, then the student name (`title-2`), program and term (`label`), a status pill, the amount due (`title-1`, tabular) and "Due Mon 14 Oct". The whole card is one tap target that opens the statement.
- **Line item:** head name left, amount right (tabular), and a chevron if a trace exists. Tapping expands the **calculation trace** inline in an accordion (`--surface-sunken`, `label` size, e.g. `15 credit hours × Rs 9,500 = Rs 142,500`).
- **Instalment:** number, due date, amount and status, each with its own Pay action when payable (§7.5).

### 5.3 Status pills: one vocabulary

The status labels **come from `CONTRACT.md`**. Design only maps them to a look. Agents never invent new status words.

| Contract state | Pill | Icon (Lucide) |
|---|---|---|
| Invoice `Paid` | success-soft / success | `check-circle-2` |
| Invoice `PartiallyPaid` | accent-soft / accent | `circle-dashed` |
| Invoice `Issued` (due in >7 days) | neutral | `calendar` |
| Due in ≤7 days (derived) | warning-soft / warning | `clock` |
| Overdue (derived, §6.5) | danger-soft / danger | `alert-circle` |
| Attempt `Pending` | warning-soft / warning | `loader` (spinning) |
| Attempt `Failed` / `Expired` | danger-soft / danger | `x-circle` |
| Clearance `Cleared` / `Cleared (deferment approved)` / `Not cleared` | success / success / danger | `shield-check` / `shield-check` / `shield-alert` |

Every pill has an icon **and** text and is readable by a screen reader as one phrase.

### 5.4 Forms and inputs

- Visible label above every field. Placeholders are examples, never labels.
- Input height is at least 48 px and the text is 16 px.
- Use semantic input types (`tel`, `inputmode="numeric"`) and `autocomplete="one-time-code"` for OTP.
- Validate on blur, not on every keystroke. The error sits under the field, linked with `aria-describedby` and announced with `role="alert"`.
- **Error copy says what happened and what to do next:** "This number isn't registered with any institution yet. Check with your university's finance office, or ask your child to send you a link code."
- After a failed submit with several errors, focus a summary that links to each field.

### 5.5 Sheets, dialogs, toasts

- **Bottom sheet** (Vaul-style) for mobile choices such as "Other ways to pay", instalment picker and share link. It enters with `translateY(100%) → 0` over `--dur-sheet` `--ease-drawer`, supports drag to dismiss with momentum (velocity > 0.11 dismisses) and damping past the top, and **always has a visible close button** as well as the gesture.
- **Dialog** only to confirm something irreversible. Dialogs stay centred with `transform-origin: center`. They scale from `0.96` plus opacity, **never from `scale(0)`**.
- **Popovers and dropdowns** scale from their trigger (`transform-origin: var(--radix-popover-content-transform-origin)`).
- **Toasts** use Sonner, placed bottom-centre above the action bar with `aria-live="polite"`. Auto-dismiss after 4 s. Pause the timer while the tab is hidden. Toasts never steal focus. **Payment results are never toast-only.** They get a full status screen.

### 5.6 Loading, empty, error, offline

| State | Rule |
|---|---|
| Loading < 300 ms | Show nothing, so there is no flash |
| Loading > 300 ms | **Skeletons in the exact final layout** with the same heights, so there is zero layout shift. Spinners are allowed only inside buttons. |
| Empty | An illustration-free, friendly sentence plus the next action. For example: "No fees due. You're all set for Fall 2026." (see §7.4 all-clear) |
| Error | A message rendered from the **contract error payload**, plus Retry plus a help route. **A blank area or a missing message is a Gate failure.** This is §16.1's exact bug. |
| Offline | A slim top banner reading "You're offline. Showing what we last loaded." Show cached statements read-only and disable Pay with a reason. |

---

## 6. Screen blueprints: parent mobile web (P2–P3), in build order

Each blueprint lists **goal → layout → must-have states**. F owns them. They cover the MVP screens in §9.2 of the plan plus the payment return and pay-on-behalf pages.

### 6.1 OTP login
- **Goal:** get in within 20 seconds, with no password ever.
- **Layout:** the institution logo if the user arrived from a tenant deep link (otherwise the product mark), then the headline "Enter your mobile number". Use one `tel` field with a fixed `+92` prefix and format as the user types (`3XX XXXXXXX`), then a sticky "Send code" button.
- **OTP step:** six separate boxes that accept paste and **autofill** (`one-time-code`) and submit automatically on the sixth digit. "Resend in 0:45" counts down in tabular figures. "Wrong number? Edit" goes back and keeps the number.
- **States:** rate-limited ("Too many tries. Try again in 10 minutes." with a countdown), a wrong code (the boxes shake with a 6 px `translateX` over 3 cycles in 300 ms, **suppressed under reduced motion**, and the message is announced), and a number with no linked students (explain the link-code fallback, §9.1).

### 6.2 Home: the family view (L6)
- **Goal:** *"How much do I owe, for whom, and by when?"* answered at a glance.
- **Layout:**
  1. Greeting row ("Assalam-o-Alaikum, Imran", or the Urdu equivalent) plus the language toggle. Keep it quiet.
  2. **Summary hero card:** "Total due across your family" in `display-amount`, then "Next due: Mon 14 Oct · Hamza · Rs 71,250".
  3. **Grouped by institution** (L6): an institution header (logo chip plus name) with its student cards (§5.2) underneath.
  4. A clearance banner **only when at risk**: "Hamza's exam clearance needs Rs 71,250 by Fri 18 Oct."
- **Sticky bar:** "Pay next due · Rs 71,250", which deep-links to the Pay flow for that item. Because of L1, each institution is a separate checkout. The UI never implies a combined payment. A parent paying two institutions gets two clear, sequential checkouts ("1 of 2 paid").
- **States:** all paid (§7.4 all-clear moment), a first visit with students auto-linked ("We found 2 students linked to your number."), and no students (link-code instructions).

### 6.3 Student statement
- **Goal:** trust. The parent understands every rupee.
- **Layout:** the student and term header plus the clearance pill, then the **amount-due hero**, then a "**What changed since last term**" card (per-head deltas and the total % change from the stored diff, §7.6, e.g. "Tuition +8.2%", with up and down arrows **and** words). Then the **line items** with expandable traces, discounts shown as negative lines in success colour, a total row, and then instalments if a plan exists.
- **Sticky bar:** "Pay Rs X" for the next payable instalment, or the full balance.
- **Secondary:** "Download statement (PDF)" and "Request more time" (deferment), both as ghost buttons *below* the content.
- **Never show:** a tax line (§7.4), a zero line, or internal rule codes.
- An adjustment or superseded invoice shows a banner: "This statement was updated on 2 Oct. Here's what changed." It links to the adjustment. The UI never silently swaps numbers.

### 6.4 Pay: the most important screen in the product (L7, §6.3)
- **Step 1, choose what to pay:** instalment or full balance, as radio cards (tap anywhere on the card). The amount is a label, never an editable field, unless the tenant allows partial payment.
- **Step 2, how to pay:**
  - One **large recommended card** for the tenant's highest enabled tier (for example **Raast**, "Instant · No extra charge"), with a subtle "Recommended" pill.
  - Below it, a collapsed **"Other ways to pay"** row that opens a bottom sheet listing the lower tiers with honest meta: "Card · Opens secure checkout · Bank fee may apply" and "Bank voucher · Pay at any bank app, ATM or branch".
  - **Never four equal tiles.** Never pre-select a card when a Tier 1 channel exists.
- **Step 3, confirm:** a summary card (institution, student, term, amount), then the sticky "**Pay Rs 71,250**" button.
- **Hand-off:** if the channel leaves our surface (hosted page or bank app), show an interstitial *before* leaving: "Opening JazzCash… When you're done, you'll come right back here." It opens in a system browser tab or through the provider SDK. **Never a WebView** (§6.6).
- **Trust cues** (placed near the button, not in a footer): institution logo, "Paid directly to [University] · A2Z never holds your money" (true because of L1), and a lock icon plus the channel mark.

### 6.5 Payment status / return page
The return URL is **not proof of payment**. The page asks the API (§6.4 step 6), so this page needs a designed **pending** state:
- **Pending:** a calm animated indicator (a ring spinning at `linear` 900 ms per turn) with "Confirming with your bank… This usually takes a few seconds." After 20 s: "Still confirming. You can safely close this page. We'll send the receipt on WhatsApp." **No retry button that could create a second payment.**
- **Succeeded:** the delight moment (§7.4), with the receipt number, amount, date, channel, a "Download receipt" button and "Share on WhatsApp".
- **Failed / Expired:** a plain reason, no money taken (only when the API confirms that), and "Try again" plus "Try another way".
- **Voucher (Tier 3):** a large consumer number set in tabular, tracking-wide text with a one-tap **Copy** button ("Copied" check for 1.5 s), the amount, and step-by-step "Pay in your bank app" instructions. The live status updates on its own when the callback lands.

### 6.6 Payment history + receipt
- Payments are grouped by term, newest first. Each row shows the amount, date, student, channel and a status pill.
- The receipt view is a clean paper-like card with the institution header, lines paid, receipt number, gateway reference and QR. It offers "Download PDF" and "Share".

### 6.7 Deferment request
- A short form: which item, a reason (chips plus an optional note) and a proposed date (a native date picker limited to allowed dates). The primary button reads "Send request".
- The status timeline reads **Requested → Under review → Approved / Declined**, and each decision shows the finance office's note.

### 6.8 Share pay-on-behalf link
- A preview of exactly what the recipient will see, then "Share on WhatsApp" (the primary action, using the Web Share API with a WhatsApp fallback), then "Copy link", then the expiry ("Link works until Sun 20 Oct").
- Revoking a link is **destructive** and needs a confirmation.

### 6.9 Pay-on-behalf page (no account)
- It shows **only** the institution (with branding), the student's **initials**, the term, the amount and the due date (§9.3), plus "Requested by a family member".
- The same Pay flow as §6.4. There is no login and no upsell before payment. An "Install the app" suggestion may appear after success, once and softly.

### 6.10 Settings
- Language (English / اردو) switches instantly with no reload, and the layout flips direction with no animation.
- Notification channels have per-channel toggles and quiet hours. Profile follows.
- Log out is visually separated at the bottom.

---

## 7. Motion and delight (the Emil Kowalski framework, applied)

### 7.1 Decide by frequency before you animate

| Surface | How often | Motion budget |
|---|---|---|
| **Parent app** | A few sessions per term | Standard motion. Delight is allowed at milestones. |
| Parent: tab switch, list scroll, trace expand | Tens of times per session | Minimal: 160–220 ms, no stagger after the first load |
| **Portal** | Hundreds of actions a day | **Near zero.** No page transitions, no stagger, instant table updates. Only press feedback, dropdown and dialog entry (≤200 ms). |
| Keyboard-triggered actions (portal shortcuts, command palette) | Constant | **No animation, ever** |

Every animation must answer **"why does this move?"** with one of these purposes: spatial consistency, state indication, feedback, or preventing a jarring change. "It looks cool" is not an answer on a surface people use often.

### 7.2 Standard patterns (copy these, don't reinvent them)

| Interaction | Spec |
|---|---|
| Button / card press | `scale(0.97)` / `scale(0.98)`, `--dur-press`, `--ease-out` |
| Trace accordion expand | Content: opacity 0→1 plus `translateY(-4px)→0`, `--dur-base` `--ease-out`. The height change uses the Radix/Collapsible CSS var. |
| Bottom sheet | `translateY(100%)→0`, `--dur-sheet`, `--ease-drawer`. Exit at `--dur-exit`. |
| Dialog | Opacity plus `scale(0.96→1)`, 200 ms `--ease-out`, centred origin |
| Toast | Enter from the bottom with `translateY(100%)`, **CSS transitions** (interruptible), not keyframes |
| Page enter (parent, first load only) | Cards stagger 40 ms apart (opacity plus `translateY(8px)`), 300 ms total cap. **Never blocks taps.** |
| Status pill change | Crossfade with a 2 px blur during the swap, 200 ms |
| Numbers updating (paid amount) | Crossfade the old and new values. No odometer rolls on money, because a parent shouldn't see a wrong number even for a moment. |
| Pending spinner | `linear`, fast (≈900 ms per turn). A faster spinner makes the wait *feel* shorter. |

**Enter/exit asymmetry:** exits are about 60–70% of the enter duration. The system responds fast, while the user deliberates at their own pace.

### 7.3 Technical rules
- Use CSS transitions for UI state and `@starting-style` for mount animations (with a `data-mounted` fallback).
- If you use Motion (framer), use `transform: "translateX()"` strings rather than the `x`/`y` shorthands for anything that runs during page load, because the shorthands drop frames on a busy main thread.
- Use `prefers-reduced-motion: reduce` to remove all translate, scale and shake movement. Keep opacity and colour fades, since they aid comprehension.
- Hover animations sit inside `@media (hover: hover) and (pointer: fine)`.
- Test on a real mid-range Android over throttled "Fast 3G". Review animations the next day, slowed down to 0.25× in DevTools.

### 7.4 Delight moments: the "wow" budget

Delight is reserved for **rare, emotional** moments. There are exactly four:

1. **Payment succeeded.** A green check draws itself (SVG `stroke-dashoffset`, 400 ms `--ease-out`), the receipt card rises from `translateY(16px)` with a soft spring (`bounce: 0.15`), and a success haptic plays on native (P4). The text reads: "Paid. Rs 71,250 received by [University]. Receipt #R-2026-004812." This is the moment the parent tells another parent about.
2. **All clear.** When every child's fees are paid for the term, the Home hero becomes a calm success state: "You're all set for Fall 2026 ✓ Next statement expected in January." (The ✓ is a Lucide icon, not text.)
3. **First link.** "We found Hamza and Ayesha" introduces the student cards with the stagger.
4. **Clearance secured.** When a payment flips clearance to Cleared, the pill changes with a single soft pulse.

No confetti on money. No looping animations. Nothing moves for more than about 600 ms. Under reduced motion, every moment degrades to a fade.

---

## 8. Copy and tone

- **Warm and plain.** Aim for an 8th-grade reading level in English. Urdu copy is **written by a native speaker**, not machine-translated. The supervisor flags any string that looks machine-translated.
- **Lead with the verb and the amount:** "Pay Rs 71,250", "Download receipt", "Request more time".
- **Never blame the parent.** Write "That code didn't match. Try again or resend.", not "Invalid OTP".
- **Always give dates with the day name** ("Mon 14 Oct") and relative time for anything soon ("in 5 days").
- **Keep jargon out of the parent UI.** Say "statement", not "invoice", and "instalment", not "part". Avoid "PaymentAttempt", "adapter", "tenant" and "superseded". The contract enums map to parent-friendly labels in one i18n file.
- **Keep money honest.** If a channel has a fee, say so *before* the parent taps. Never surprise them at the gateway.

---

## 9. Institution portal (agent E): a different density, the same family

The same tokens, fonts, icons and status pills are used, tuned for **speed at a desk**:
- **Layout:** a 240 px left sidebar (collapsible), a top bar with tenant switcher, search and user menu, and content at `max-w-7xl`. Breadcrumbs appear for anything nested three or more levels deep.
- **Density:** body 14–15 px, table rows 44 px, the dense spacing scale.
- **Tables:** tabular figures, amounts right-aligned, sticky headers, sortable columns with `aria-sort`, and filters that persist in the URL. Lists over 50 rows are virtualised. Bulk actions appear in a contextual bar.
- **Keyboard first:** full tab order, visible focus rings (2 px `--accent` with a 2 px offset), a `/` shortcut for search, and a ⌘K command palette with **no open or close animation**.
- **Destructive financial actions** (waive, reverse, resolve a recon exception) require a typed or explicit confirmation plus a reason field, because they are audit-logged (§13).
- **Import wizard** (§8.3): a stepper that shows Upload → Map → Dry run → Commit. The dry-run result uses big count tiles (create, update, skip, error), and the error report is one click away.
- **Dashboards:** use the ui-ux-pro-max `chart` guidance. Use a line chart for trends around deadlines and a bar chart for program comparisons. Always pair a chart with a table alternative and direct labels, and never use a pie chart with more than five slices.
- **Preview before issuing** (§7.6): outliers are highlighted with an icon and text, never with colour alone.

---

## 10. Accessibility and performance (non-negotiable)

**Accessibility (WCAG 2.2 AA minimum):**
- Text contrast is at least 4.5:1, and non-text UI (borders, icons, focus rings) at least 3:1. Check it in **both** themes.
- Focus rings are never removed. Sticky bars must not hide the focused element.
- Screen-reader order matches visual order. After a route change, focus moves to the main heading.
- Status changes (payment confirmed, OTP sent) are announced through **one** `aria-live="polite"` region as a complete phrase.
- Every drag or swipe has a button alternative.
- OTP entry allows paste and autofill (accessible authentication).
- The browser must never block zoom.

**Performance budget (parent web on mid-range Android over 4G):**
- LCP under 2.0 s, INP under 150 ms, CLS under 0.05.
- The first-load JS for the Home route is under 170 KB gzipped.
- Use `next/image` with explicit dimensions for every image. Institution logos are SVG or WebP, cached.
- Tap feedback appears within 100 ms. The UI responds optimistically wherever the contract allows. Payments never do.

---

## 11. The Design Gate (added to the supervisor gate, §16.4)

A UI phase (P1-portal, P2, P3, P4) is **not done** until S has completed every check below and appended a written sign-off to the phase log. A partial pass is a failed pass.

1. **Token audit:** no raw hex, px spacing outside the scale, raw `ms`, `transition: all` or `ease-in` in feature code. This can be grep-driven.
2. **Component audit:** no local copies of `packages/ui` primitives. All status pills map to `CONTRACT.md` states.
3. **One primary action per screen:** checked on every screen in §6.
4. **State matrix:** every screen screenshotted in loading, empty, error (using a real contract error payload), offline, long-content and success states.
5. **Direction and language:** every screen in English LTR and Urdu RTL, with Nastaliq unclipped.
6. **Viewports:** 360, 412, 768 and 1280 widths, plus 200% text scale and landscape.
7. **Accessibility:** automated axe pass, a keyboard-only walkthrough and a screen-reader walkthrough of the Pay journey.
8. **Motion:** reduced-motion pass, frequency check (§7.1), and an on-device check on a real Android phone.
9. **Pay journey trace (P3):** Home → Statement → Pay → hand-off → return pending → success → receipt on every enabled tier. Confirm the default channel is the highest tier, "Other ways" is collapsed, no card field of our own exists and there is no WebView.
10. **Scope check:** no tax line, wallet balance, BNPL or school-only UI anywhere.
11. **The five-second test:** show Home to someone outside the team for five seconds. They must be able to say what is owed and by when. If they can't, the hierarchy failed.

---

## 12. How S reviews a PR

Use **one markdown table**, one row per issue, and nothing else:

| Before | After | Why |
| --- | --- | --- |
| `transition: all 300ms` | `transition: transform var(--dur-base) var(--ease-out)` | Name the properties; `all` animates layout |
| `<Button>Continue</Button>` on Pay | `<Button>Pay Rs 71,250</Button>` | The label is the verb and the amount |
| Four channel tiles of equal size | One recommended card + "Other ways to pay" sheet | §6.3 agent rule; hand-off loss |
| `ml-4` on the icon | `ms-4` | Breaks in Urdu RTL |
| Error area renders empty on 422 | Render `error.message` from the contract payload plus Retry | §16.1: a blocked action must never fail silently |
| `scale(0)` on the dialog | `scale(0.96)` plus `opacity: 0` | Nothing appears from nothing |
| `text-[#1D4ED8]` | `text-accent` | Tokens only |

Findings go to the **owning agent** only (§16.5). S never fixes code in someone else's path.

---

## 13. Memory and token conservation (claude-mem)

Agents lose context between sessions. Re-deriving design decisions wastes tokens and, worse, produces **drift**: the design version of two agents holding contradictory beliefs.

**At the start of every UI task:**
1. Read **§0** of this file, then **only** the section for your screen (§6.x) or component (§5.x). Don't reload the whole file every time.
2. If claude-mem is available, **search it first**:
   - `search` with a query such as `"pay screen channel tier"` or `"status pill"` to get an index of past decisions.
   - `timeline` around the relevant hit for context.
   - `get_observations` for **only** the IDs you need. Do not bulk-fetch.
3. Check whether `packages/ui` already exports what you need before you design anything.

**When you make or receive a design decision:**
- Record it in one line: *what was decided, why, and which section of this file it refines*. For example: "Voucher number uses 4-4-4 grouping for readability (refines §6.5)."
- If a decision **changes** this file's rules, it is not yours to record as fact. Raise it to S. A memory is never a substitute for a change to this file.
- Never store secrets, parent data, phone numbers or tenant credentials in memory.

**Token hygiene:**
- Reference sections by number ("per §5.3") rather than pasting them into prompts.
- Screenshot only the states being reviewed, not every state on every iteration.
- Query ui-ux-pro-max with a focused search, meaning one intent and a `--domain` or `--stack` (e.g. `"bottom sheet focus" --stack shadcn`), rather than regenerating a design system. **The design system is already decided here.** Do not run `--design-system` again, because it returns a different palette and will cause drift.

---

## 14. Quick reference: where to look

| Question | Go to |
|---|---|
| What does this status look like? | §5.3 |
| Where does the Pay button go? | §4.1, §6.4 |
| How long should this animation be? | §3.5, §7.2 |
| Can I animate this at all? | §7.1 |
| How do I format money? | §3.3 (and `packages/money`) |
| What does the error state show? | §5.6 and the error shape in `CONTRACT.md` |
| Tenant brand colour on a button? | **No.** §3.2 |
| RTL rules | §4.3 |
| Portal differences | §9 |
| Is my screen done? | §11 |

---

*When in doubt, take something away. The parent came here to pay a fee, not to admire an interface. If it works so well that they don't think about it, and it looks good enough that they trust it at first glance, that is the wow.*
