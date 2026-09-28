"use client";

import { clsx, type ClassValue } from "clsx";
import { X } from "lucide-react";
import {
  useEffect,
  useId,
  useRef,
  useState,
  type FormEvent,
  type ReactNode,
} from "react";
import { createPortal } from "react-dom";
import { Button } from "./Button";

function cx(...inputs: ClassValue[]) {
  return clsx(inputs);
}

export interface ConfirmDialogProps {
  open: boolean;
  title: string;
  description?: ReactNode;
  /** Primary action label (e.g. "Reverse payment"). */
  confirmLabel: string;
  cancelLabel?: string;
  /**
   * When true, user must enter a non-empty reason before confirm is enabled
   * (portal destructive financial actions — DESIGN §9).
   */
  requireReason?: boolean;
  reasonLabel?: string;
  reasonPlaceholder?: string;
  /** Use destructive fill for the primary confirm button. */
  destructive?: boolean;
  /** Portal-dense sizing (44px controls). Default true. */
  dense?: boolean;
  busy?: boolean;
  onConfirm: (reason?: string) => void;
  onCancel: () => void;
}

/**
 * Centred confirm dialog — DESIGN_SUPERVISOR §5.5 / §9.
 * Motion: transform + opacity only, ≤200ms via `--dur-fast`, `--ease-out`.
 * Never scale(0). Escape + visible close.
 */
export function ConfirmDialog({
  open,
  title,
  description,
  confirmLabel,
  cancelLabel = "Cancel",
  requireReason = false,
  reasonLabel = "Reason",
  reasonPlaceholder = "Required for the audit log",
  destructive = true,
  dense = true,
  busy = false,
  onConfirm,
  onCancel,
}: ConfirmDialogProps) {
  const titleId = useId();
  const descId = useId();
  const reasonId = useId();
  const reasonRef = useRef<HTMLTextAreaElement>(null);
  const [reason, setReason] = useState("");
  const [mounted, setMounted] = useState(false);
  const [entered, setEntered] = useState(false);

  useEffect(() => {
    setMounted(true);
  }, []);

  useEffect(() => {
    if (!open) {
      setEntered(false);
      setReason("");
      return;
    }

    const frame = requestAnimationFrame(() => setEntered(true));
    const prevOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";

    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape" && !busy) onCancel();
    };
    window.addEventListener("keydown", onKey);

    if (requireReason) {
      queueMicrotask(() => reasonRef.current?.focus());
    }

    return () => {
      cancelAnimationFrame(frame);
      document.body.style.overflow = prevOverflow;
      window.removeEventListener("keydown", onKey);
    };
  }, [open, busy, onCancel, requireReason]);

  if (!mounted || !open) return null;

  const reasonTrimmed = reason.trim();
  const canConfirm = !requireReason || reasonTrimmed.length > 0;
  const size = dense ? "portal" : "mobile";

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    if (!canConfirm || busy) return;
    onConfirm(requireReason ? reasonTrimmed : undefined);
  };

  return createPortal(
    <div
      className="fixed inset-0 z-[var(--z-dialog)] flex items-center justify-center p-[var(--space-16)]"
      role="presentation"
    >
      <button
        type="button"
        aria-label="Dismiss"
        className={cx(
          "absolute inset-0 border-0 bg-[rgb(15_23_42_/_.4)]",
          "transition-opacity duration-[var(--dur-fast)] ease-[var(--ease-out)]",
          entered ? "opacity-100" : "opacity-0"
        )}
        onClick={() => {
          if (!busy) onCancel();
        }}
      />
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        aria-describedby={description ? descId : undefined}
        className={cx(
          "relative w-full max-w-[420px]",
          "rounded-[var(--radius-12)] bg-[var(--surface)] shadow-[var(--shadow-3)]",
          "border border-[var(--border)]",
          dense ? "p-[var(--space-16)]" : "p-[var(--space-20)]",
          "origin-center",
          "transition-[transform,opacity] duration-[var(--dur-fast)] ease-[var(--ease-out)]",
          entered
            ? "translate-y-0 scale-100 opacity-100"
            : "translate-y-[var(--space-4)] scale-[0.96] opacity-0"
        )}
      >
        <div className="mb-[var(--space-12)] flex items-start justify-between gap-[var(--space-12)]">
          <h2
            id={titleId}
            className={cx(
              dense
                ? "text-[15px] leading-5 font-semibold text-[var(--ink)]"
                : "font-[var(--font-title-2)] text-[var(--ink)]"
            )}
          >
            {title}
          </h2>
          <button
            type="button"
            aria-label="Close"
            disabled={busy}
            onClick={onCancel}
            className={cx(
              "shrink-0 touch-manipulation rounded-[var(--radius-8)]",
              "text-[var(--ink-secondary)]",
              "min-h-[44px] min-w-[44px] inline-flex items-center justify-center",
              "transition-opacity duration-[var(--dur-press)] ease-[var(--ease-out)]",
              "hover:opacity-80 active:scale-[0.97]",
              "focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--accent)]",
              "disabled:opacity-50 disabled:pointer-events-none"
            )}
          >
            <X size={18} strokeWidth={1.75} aria-hidden="true" />
          </button>
        </div>

        {description ? (
          <div
            id={descId}
            className={cx(
              "mb-[var(--space-16)] text-[var(--ink-secondary)]",
              dense
                ? "text-[14px] leading-5"
                : "font-[var(--font-body)]"
            )}
          >
            {description}
          </div>
        ) : null}

        <form onSubmit={handleSubmit} className="flex flex-col gap-[var(--space-16)]">
          {requireReason ? (
            <div className="flex flex-col gap-[var(--space-4)]">
              <label
                htmlFor={reasonId}
                className={cx(
                  "text-[var(--ink)]",
                  dense
                    ? "text-[14px] leading-5 font-medium"
                    : "font-[var(--font-label)]"
                )}
              >
                {reasonLabel}
              </label>
              <textarea
                ref={reasonRef}
                id={reasonId}
                name="reason"
                required
                rows={3}
                value={reason}
                disabled={busy}
                placeholder={reasonPlaceholder}
                onChange={(e) => setReason(e.target.value)}
                className={cx(
                  "w-full resize-y rounded-[var(--radius-8)]",
                  "border border-[var(--border-strong)] bg-[var(--surface)]",
                  "px-[var(--space-12)] py-[var(--space-8)]",
                  "text-[16px] leading-6 text-[var(--ink)]",
                  "placeholder:text-[var(--ink-tertiary)]",
                  "focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--accent)]",
                  "disabled:opacity-50"
                )}
              />
            </div>
          ) : null}

          <div
            className={cx(
              "flex flex-wrap items-center justify-end gap-[var(--space-8)]",
              "pt-[var(--space-4)]"
            )}
          >
            <Button
              type="button"
              variant="secondary"
              size={size}
              disabled={busy}
              onClick={onCancel}
            >
              {cancelLabel}
            </Button>
            <Button
              type="submit"
              variant={destructive ? "destructive" : "primary"}
              size={size}
              loading={busy}
              disabled={!canConfirm}
              className={
                destructive
                  ? "bg-[var(--danger)] text-[var(--on-ink)] border-[var(--danger)]"
                  : undefined
              }
            >
              {confirmLabel}
            </Button>
          </div>
        </form>
      </div>
    </div>,
    document.body
  );
}
