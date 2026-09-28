import { clsx, type ClassValue } from "clsx";
import {
  AlertCircle,
  Calendar,
  CheckCircle2,
  CircleDashed,
  Clock,
  Loader2,
  ShieldAlert,
  ShieldCheck,
  XCircle,
  type LucideIcon,
} from "lucide-react";

function cx(...inputs: ClassValue[]) {
  return clsx(inputs);
}

export type StatusTone = "success" | "warning" | "danger" | "accent" | "neutral";

/**
 * CONTRACT §2 + derived display statuses only — no free-text labels (CONTRACT §6.4.8).
 */
export type StatusPillKind =
  | "Draft"
  | "Paid"
  | "PartiallyPaid"
  | "Issued"
  | "DueSoon"
  | "Overdue"
  | "Created"
  | "Pending"
  | "Succeeded"
  | "Failed"
  | "Expired"
  | "Cancelled"
  | "Reversed"
  | "Cleared"
  | "ClearedWithDeferment"
  | "NotCleared";

const toneClass: Record<StatusTone, string> = {
  success: "bg-[var(--success-soft)] text-[var(--success)]",
  warning: "bg-[var(--warning-soft)] text-[var(--warning)]",
  danger: "bg-[var(--danger-soft)] text-[var(--danger)]",
  accent: "bg-[var(--accent-soft)] text-[var(--accent)]",
  neutral: "bg-[var(--surface-sunken)] text-[var(--ink-secondary)]",
};

const kindConfig: Record<
  StatusPillKind,
  { tone: StatusTone; label: string; Icon: LucideIcon; spin?: boolean }
> = {
  Draft: { tone: "neutral", label: "Draft", Icon: CircleDashed },
  Paid: { tone: "success", label: "Paid", Icon: CheckCircle2 },
  PartiallyPaid: {
    tone: "accent",
    label: "Partially paid",
    Icon: CircleDashed,
  },
  Issued: { tone: "neutral", label: "Issued", Icon: Calendar },
  DueSoon: { tone: "warning", label: "Due soon", Icon: Clock },
  Overdue: { tone: "danger", label: "Overdue", Icon: AlertCircle },
  Created: { tone: "neutral", label: "Created", Icon: CircleDashed },
  Pending: { tone: "warning", label: "Pending", Icon: Loader2, spin: true },
  Succeeded: { tone: "success", label: "Succeeded", Icon: CheckCircle2 },
  Failed: { tone: "danger", label: "Failed", Icon: XCircle },
  Expired: { tone: "danger", label: "Expired", Icon: XCircle },
  Cancelled: { tone: "neutral", label: "Cancelled", Icon: XCircle },
  Reversed: { tone: "warning", label: "Reversed", Icon: AlertCircle },
  Cleared: { tone: "success", label: "Cleared", Icon: ShieldCheck },
  ClearedWithDeferment: {
    tone: "success",
    label: "Cleared (deferment approved)",
    Icon: ShieldCheck,
  },
  NotCleared: { tone: "danger", label: "Not cleared", Icon: ShieldAlert },
};

export interface StatusPillProps {
  kind: StatusPillKind;
  className?: string;
}

export function StatusPill({ kind, className }: StatusPillProps) {
  const cfg = kindConfig[kind];
  const Icon = cfg.Icon;
  return (
    <span
      className={cx(
        "inline-flex items-center gap-[var(--space-4)]",
        "rounded-[var(--radius-8)] px-[var(--space-8)] py-[var(--space-4)]",
        "font-[var(--font-label)]",
        toneClass[cfg.tone],
        className
      )}
      role="status"
      aria-label={cfg.label}
    >
      <Icon
        size={16}
        strokeWidth={1.75}
        aria-hidden="true"
        className={cfg.spin ? "animate-spin" : undefined}
      />
      <span>{cfg.label}</span>
    </span>
  );
}
