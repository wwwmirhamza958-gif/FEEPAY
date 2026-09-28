import { clsx, type ClassValue } from "clsx";
import type { ReactNode } from "react";

export function cx(...inputs: ClassValue[]) {
  return clsx(inputs);
}

/** Phone product shell: design at 360px, centred max 480 on desktop. */
export function PhoneShell({
  children,
  className,
}: {
  children: ReactNode;
  className?: string;
}) {
  return (
    <div
      className={cx(
        "mx-auto flex min-h-dvh w-full max-w-[480px] flex-col bg-canvas text-ink",
        className
      )}
    >
      {children}
    </div>
  );
}

/** Sticky thumb-zone action bar — DESIGN_SUPERVISOR §4.1 */
export function StickyActionBar({
  children,
  className,
}: {
  children: ReactNode;
  className?: string;
}) {
  return (
    <div
      className={cx(
        "sticky bottom-0 z-fee-action border-t border-border bg-surface shadow-fee-2",
        "px-[var(--space-16)] pt-[var(--space-12)]",
        "pb-[calc(var(--space-12)+env(safe-area-inset-bottom))]",
        className
      )}
    >
      {children}
    </div>
  );
}

export function MockBanner() {
  return (
    <p
      className="bg-surface-sunken px-[var(--space-16)] py-[var(--space-8)] text-center font-[var(--font-micro)] text-ink-tertiary"
      role="note"
    >
      Mock data — parent API pending (see REQUESTS_TO_C_FROM_F / REQUESTS_TO_D)
    </p>
  );
}
