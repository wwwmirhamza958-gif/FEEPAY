"use client";

import { Button } from "@fee/ui";
import { clsx } from "clsx";
import { useEffect, useId, useRef, type ReactNode } from "react";

type ConfirmDialogProps = {
  open: boolean;
  title: string;
  description: string;
  /** Required for destructive financial actions — DESIGN §9 */
  reason: string;
  onReasonChange: (value: string) => void;
  reasonLabel?: string;
  confirmLabel: string;
  confirmVariant?: "primary" | "destructive";
  confirming?: boolean;
  onConfirm: () => void;
  onCancel: () => void;
  children?: ReactNode;
};

export function ConfirmDialog({
  open,
  title,
  description,
  reason,
  onReasonChange,
  reasonLabel = "Reason (required for audit)",
  confirmLabel,
  confirmVariant = "destructive",
  confirming = false,
  onConfirm,
  onCancel,
  children,
}: ConfirmDialogProps) {
  const titleId = useId();
  const panelRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;
    const prev = document.activeElement as HTMLElement | null;
    panelRef.current?.querySelector<HTMLElement>("textarea, button")?.focus();
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") onCancel();
    };
    window.addEventListener("keydown", onKey);
    return () => {
      window.removeEventListener("keydown", onKey);
      prev?.focus();
    };
  }, [open, onCancel]);

  if (!open) return null;

  const canConfirm = reason.trim().length >= 3 && !confirming;

  return (
    <div
      className="fee-overlay fixed inset-0 z-fee-dialog flex items-center justify-center p-[var(--space-16)]"
      role="presentation"
      onMouseDown={(e) => {
        if (e.target === e.currentTarget) onCancel();
      }}
    >
      <div
        ref={panelRef}
        role="alertdialog"
        aria-modal="true"
        aria-labelledby={titleId}
        className={clsx(
          "fee-dialog-panel w-full max-w-lg rounded-fee-16 bg-surface p-[var(--space-24)] shadow-fee-3"
        )}
      >
        <h2 id={titleId} className="font-heading text-[length:20px] leading-7 font-semibold text-ink">
          {title}
        </h2>
        <p className="mt-[var(--space-8)] text-[15px] leading-[22px] text-ink-secondary">
          {description}
        </p>
        {children}
        <label className="mt-[var(--space-16)] block">
          <span className="text-[14px] font-medium text-ink">{reasonLabel}</span>
          <textarea
            value={reason}
            onChange={(e) => onReasonChange(e.target.value)}
            rows={3}
            className="mt-[var(--space-8)] w-full rounded-fee-8 border border-border-strong bg-surface-sunken px-[var(--space-12)] py-[var(--space-8)] text-[15px] text-ink focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
            required
          />
        </label>
        <div className="mt-[var(--space-20)] flex flex-wrap justify-end gap-[var(--space-8)]">
          <Button size="portal" variant="secondary" onClick={onCancel} disabled={confirming}>
            Cancel
          </Button>
          <Button
            size="portal"
            variant={confirmVariant}
            onClick={onConfirm}
            disabled={!canConfirm}
            loading={confirming}
          >
            {confirmLabel}
          </Button>
        </div>
      </div>
    </div>
  );
}
