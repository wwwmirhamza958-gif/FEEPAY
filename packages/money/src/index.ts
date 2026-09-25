/**
 * PKR money helpers — CONTRACT.md §1 / §6.1
 * Wire format: decimal strings with exactly two fractional digits ("142500.00").
 * Display: whole rupees. Never accept `number` in public APIs (float forbidden).
 */

export type Money = string & { readonly __brand: "Money" };

const TRUE_MINUS = "\u2212";
const THIN_SPACE = "\u2009";
const DECIMAL_PATTERN = /^-?\d+\.\d{2}$/;

function assertDecimalString(value: string): asserts value is Money {
  if (!DECIMAL_PATTERN.test(value)) {
    throw new Error(`Invalid money decimal string: ${value}`);
  }
}

/** Construct Money from a decimal string. Rejects number (CONTRACT §6.1). */
export function money(value: string): Money {
  assertDecimalString(value);
  return value as Money;
}

/** @deprecated Do not use — number constructors are forbidden. */
export function moneyFromNumber(_value: number): never {
  throw new Error("Money from number is forbidden; use decimal strings (CONTRACT §1.4)");
}

export function assertPkrCurrency(code: string): void {
  if (code !== "PKR") {
    throw new Error(`Currency must be PKR; got ${code}`);
  }
}

/** Half-up to 2 decimal places (per line). Accepts intermediate precision. */
export function roundHalfUpPerLine(amount: string): Money {
  if (!/^-?\d+(\.\d+)?$/.test(amount)) {
    throw new Error(`Invalid amount for rounding: ${amount}`);
  }
  const negative = amount.startsWith("-");
  const abs = negative ? amount.slice(1) : amount;
  const [wholePart, fracRaw = ""] = abs.split(".");
  const padded = (fracRaw + "000").slice(0, 3);
  const whole = BigInt(wholePart || "0");
  const cents = BigInt(padded.slice(0, 2));
  const thousandth = Number(padded[2] ?? "0");
  let totalCents = whole * 100n + cents;
  if (thousandth >= 5) totalCents += 1n;
  if (negative) totalCents = -totalCents;
  return fromCents(totalCents);
}

function toCents(amount: Money): bigint {
  const negative = amount.startsWith("-");
  const abs = negative ? amount.slice(1) : amount;
  const [w, f] = abs.split(".");
  const cents = BigInt(w ?? "0") * 100n + BigInt(f ?? "0");
  return negative ? -cents : cents;
}

function fromCents(cents: bigint): Money {
  const negative = cents < 0n;
  const abs = negative ? -cents : cents;
  const whole = abs / 100n;
  const frac = (abs % 100n).toString().padStart(2, "0");
  const body = `${whole.toString()}.${frac}`;
  return money(negative ? `-${body}` : body);
}

export function add(a: string, b: string): Money {
  return fromCents(toCents(money(a)) + toCents(money(b)));
}

export function sub(a: string, b: string): Money {
  return fromCents(toCents(money(a)) - toCents(money(b)));
}

export function sum(amounts: string[]): Money {
  return amounts.reduce<Money>((acc, x) => add(acc, x), money("0.00"));
}

export function compare(a: string, b: string): -1 | 0 | 1 {
  const diff = toCents(money(a)) - toCents(money(b));
  if (diff < 0n) return -1;
  if (diff > 0n) return 1;
  return 0;
}

/**
 * Format for UI — whole rupees. `Rs 142,500` / `−Rs 35,625`.
 * Western digits for both `en` and `ur`.
 */
export function formatPkr(amount: string, _locale: "en" | "ur" = "en"): string {
  const m = money(amount);
  const cents = toCents(m);
  const negative = cents < 0n;
  const whole = negative ? -cents / 100n : cents / 100n;
  const grouped = whole.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",");
  const body = `Rs${THIN_SPACE}${grouped}`;
  return negative ? `${TRUE_MINUS}${body}` : body;
}

export const MONEY_CLASS = "tabular-nums";
export const ZERO: Money = money("0.00");
