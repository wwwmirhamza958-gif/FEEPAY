import { clsx } from "clsx";
import type { CSSProperties, ReactNode } from "react";

/** Amount display shell — uses packages/money formatting in feature code. */
export function Amount({
  children,
  className,
}: {
  children: ReactNode;
  className?: string;
}) {
  return (
    <span className={clsx("fee-money", "tabular-nums", className)}>{children}</span>
  );
}

export function Skeleton({ className }: { className?: string }) {
  const style: CSSProperties = { minBlockSize: 16, minInlineSize: 80 };
  return (
    <span
      aria-hidden="true"
      className={clsx(
        "inline-block animate-pulse rounded-[var(--radius-8)] bg-[var(--surface-sunken)]",
        className
      )}
      style={style}
    />
  );
}

export function EmptyState({
  title,
  action,
}: {
  title: string;
  action?: ReactNode;
}) {
  return (
    <div className="flex flex-col items-center gap-[var(--space-16)] px-[var(--space-16)] py-[var(--space-32)] text-center">
      <p className="font-[var(--font-body)] text-[var(--ink-secondary)]">{title}</p>
      {action}
    </div>
  );
}

export function ErrorState({
  message,
  onRetry,
}: {
  message: string;
  onRetry?: () => void;
}) {
  return (
    <div
      role="alert"
      className="flex flex-col items-center gap-[var(--space-12)] px-[var(--space-16)] py-[var(--space-24)] text-center"
    >
      <p className="font-[var(--font-body)] text-[var(--danger)]">{message}</p>
      {onRetry ? (
        <button
          type="button"
          onClick={onRetry}
          className="text-[var(--accent)] font-[var(--font-body-strong)] touch-manipulation"
        >
          Retry
        </button>
      ) : null}
    </div>
  );
}

/** Bottom sheet shell — full Vaul wiring later. */
export function BottomSheet({
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
  if (!open) return null;
  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-label={title}
      className="fixed inset-0 z-[var(--z-sheet)] flex items-end justify-center bg-[rgb(15_23_42_/_.4)]"
    >
      <div className="w-full max-w-[480px] rounded-t-[var(--radius-24)] bg-[var(--surface)] p-[var(--space-16)] shadow-[var(--shadow-3)]">
        <div className="mb-[var(--space-12)] flex items-center justify-between">
          <h2 className="font-[var(--font-title-2)]">{title}</h2>
          <button type="button" onClick={onClose} aria-label="Close" className="touch-manipulation">
            Close
          </button>
        </div>
        {children}
      </div>
    </div>
  );
}

/** Toast shell — Sonner wrapper lands with feature apps. */
export function Toast({ message }: { message: string }) {
  return (
    <div
      role="status"
      aria-live="polite"
      className="fixed inset-x-0 bottom-[calc(var(--space-24)+env(safe-area-inset-bottom))] z-[var(--z-toast)] mx-auto max-w-[480px] rounded-[var(--radius-12)] bg-[var(--ink)] px-[var(--space-16)] py-[var(--space-12)] text-center text-[var(--on-ink)] shadow-[var(--shadow-2)]"
    >
      {message}
    </div>
  );
}
