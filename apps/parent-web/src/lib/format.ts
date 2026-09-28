import type { StatusPillKind } from "@fee/ui";
import type { ClearanceStatus, DisplayInvoiceKind, InvoiceStatus } from "./types";

const DAY_MS = 86_400_000;

/** Format ISO date as "Mon 14 Oct" (Western digits, CONTRACT / design §4.3). */
export function formatDueDate(isoDate: string, now = new Date()): string {
  const d = parseIsoDate(isoDate);
  const weekday = d.toLocaleDateString("en-GB", { weekday: "short" });
  const day = d.getDate();
  const month = d.toLocaleDateString("en-GB", { month: "short" });
  void now;
  return `${weekday} ${day} ${month}`;
}

export function daysUntil(isoDate: string, now = new Date()): number {
  const target = parseIsoDate(isoDate);
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  return Math.round((target.getTime() - today.getTime()) / DAY_MS);
}

function parseIsoDate(isoDate: string): Date {
  const [y, m, d] = isoDate.split("-").map(Number);
  return new Date(y!, m! - 1, d!);
}

/**
 * Map invoice + derived flags to StatusPill kinds (CONTRACT §2.2 / DESIGN §5.3).
 * Parent copy for Issued stays "Issued" in the shared pill; prefer due date in nearby text.
 */
export function invoiceDisplayKind(input: {
  status: InvoiceStatus;
  isOverdue: boolean;
  dueDate: string;
  now?: Date;
}): DisplayInvoiceKind {
  if (input.status === "Paid") return "Paid";
  if (input.status === "PartiallyPaid") {
    if (input.isOverdue) return "Overdue";
    if (daysUntil(input.dueDate, input.now) <= 7) return "DueSoon";
    return "PartiallyPaid";
  }
  if (input.status === "Issued" || input.status === "Draft") {
    if (input.isOverdue) return "Overdue";
    if (daysUntil(input.dueDate, input.now) <= 7) return "DueSoon";
    return "Issued";
  }
  return "Issued";
}

export function clearancePillKind(status: ClearanceStatus): StatusPillKind {
  return status;
}

/** Format local 10-digit PK mobile as 3XX XXXXXXX while typing. */
export function formatPkMobileInput(raw: string): string {
  const digits = raw.replace(/\D/g, "").slice(0, 10);
  if (digits.length <= 3) return digits;
  return `${digits.slice(0, 3)} ${digits.slice(3)}`;
}

export function formatCountdown(totalSeconds: number): string {
  const s = Math.max(0, totalSeconds);
  const m = Math.floor(s / 60);
  const r = s % 60;
  return `${m}:${r.toString().padStart(2, "0")}`;
}
