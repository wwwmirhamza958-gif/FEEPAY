"use client";

import { useEffect, useState, type ReactNode } from "react";
import { cx } from "@/components/PhoneShell";

/**
 * Animated bottom sheet — DESIGN §5.5.
 * translateY + opacity entry via --dur-sheet / --ease-drawer; visible Close.
 */
export function ParentBottomSheet({
  open,
  title,
  children,
  onClose,
}: {
  open: boolean;
  title: string;
  children: ReactNode;
  onClose: () => void;
}) {
  const [entered, setEntered] = useState(false);

  useEffect(() => {
    if (!open) {
      setEntered(false);
      return;
    }
    const frame = requestAnimationFrame(() => setEntered(true));
    return () => cancelAnimationFrame(frame);
  }, [open]);

  if (!open) return null;

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-label={title}
      className="fixed inset-0 z-fee-sheet flex items-end justify-center"
    >
      <button
        type="button"
        aria-label="Dismiss"
        className={cx(
          "absolute inset-0 border-0 bg-[rgb(15_23_42_/_.4)]",
          "transition-opacity duration-fee-sheet ease-fee-drawer",
          entered ? "opacity-100" : "opacity-0"
        )}
        onClick={onClose}
      />
      <div
        className={cx(
          "relative w-full max-w-[480px] rounded-t-fee-24 bg-surface p-[var(--space-16)] shadow-fee-3",
          "pb-[calc(var(--space-16)+env(safe-area-inset-bottom))]",
          "transition-[transform,opacity] duration-fee-sheet ease-fee-drawer",
          entered ? "translate-y-0 opacity-100" : "translate-y-full opacity-0"
        )}
      >
        <div className="mb-[var(--space-12)] flex items-center justify-between gap-[var(--space-12)]">
          <h2 className="font-[var(--font-title-2)] text-ink">{title}</h2>
          <button
            type="button"
            onClick={onClose}
            aria-label="Close"
            className="inline-flex min-h-11 min-w-11 items-center justify-center font-[var(--font-body-strong)] text-accent touch-manipulation"
          >
            Close
          </button>
        </div>
        {children}
      </div>
    </div>
  );
}
