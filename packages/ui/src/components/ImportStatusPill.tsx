import { clsx, type ClassValue } from "clsx";
import {
  AlertCircle,
  CheckCircle2,
  CircleDashed,
  Loader2,
  RotateCcw,
  Upload,
  XCircle,
  type LucideIcon,
} from "lucide-react";
import type { StatusTone } from "./StatusPill";

function cx(...inputs: ClassValue[]) {
  return clsx(inputs);
}

/**
 * CONTRACT §2.4 Import job statuses — separate export so labels do not collide
 * with PaymentAttempt `Succeeded` / `Failed` on StatusPill (R-A-013).
 */
export type ImportStatusPillKind =
  | "Uploaded"
  | "Validating"
  | "Validated"
  | "FailedValidation"
  | "Committing"
  | "Succeeded"
  | "Failed"
  | "Reverted";

const toneClass: Record<StatusTone, string> = {
  success: "bg-[var(--success-soft)] text-[var(--success)]",
  warning: "bg-[var(--warning-soft)] text-[var(--warning)]",
  danger: "bg-[var(--danger-soft)] text-[var(--danger)]",
  accent: "bg-[var(--accent-soft)] text-[var(--accent)]",
  neutral: "bg-[var(--surface-sunken)] text-[var(--ink-secondary)]",
};

const kindConfig: Record<
  ImportStatusPillKind,
  { tone: StatusTone; label: string; Icon: LucideIcon; spin?: boolean }
> = {
  Uploaded: { tone: "neutral", label: "Uploaded", Icon: Upload },
  Validating: {
    tone: "warning",
    label: "Validating",
    Icon: Loader2,
    spin: true,
  },
  Validated: { tone: "accent", label: "Validated", Icon: CircleDashed },
  FailedValidation: {
    tone: "danger",
    label: "Failed validation",
    Icon: XCircle,
  },
  Committing: {
    tone: "warning",
    label: "Committing",
    Icon: Loader2,
    spin: true,
  },
  Succeeded: { tone: "success", label: "Succeeded", Icon: CheckCircle2 },
  Failed: { tone: "danger", label: "Failed", Icon: AlertCircle },
  Reverted: { tone: "warning", label: "Reverted", Icon: RotateCcw },
};

export interface ImportStatusPillProps {
  kind: ImportStatusPillKind;
  className?: string;
}

export function ImportStatusPill({ kind, className }: ImportStatusPillProps) {
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
