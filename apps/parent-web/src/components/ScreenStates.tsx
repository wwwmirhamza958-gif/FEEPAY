"use client";

import { Button, EmptyState, ErrorState, Skeleton } from "@fee/ui";
import type { ApiErrorEnvelope } from "@fee/api-client";
import type { ReactNode } from "react";
import { PhoneShell } from "./PhoneShell";

export function errorMessage(envelope: ApiErrorEnvelope): string {
  return envelope.error.message;
}

export function ScreenLoading({ rows = 4 }: { rows?: number }) {
  return (
    <PhoneShell>
      <div
        className="flex flex-1 flex-col gap-[var(--space-16)] px-[var(--space-16)] py-[var(--space-24)]"
        aria-busy="true"
        aria-label="Loading"
      >
        <Skeleton className="h-8 w-2/3" />
        <Skeleton className="h-24 w-full" />
        {Array.from({ length: rows }).map((_, i) => (
          <Skeleton key={i} className="h-20 w-full" />
        ))}
      </div>
    </PhoneShell>
  );
}

export function ScreenEmpty({
  title,
  actionLabel,
  onAction,
}: {
  title: string;
  actionLabel?: string;
  onAction?: () => void;
}) {
  return (
    <PhoneShell>
      <EmptyState
        title={title}
        action={
          actionLabel && onAction ? (
            <Button type="button" variant="secondary" onClick={onAction}>
              {actionLabel}
            </Button>
          ) : undefined
        }
      />
    </PhoneShell>
  );
}

export function ScreenError({
  error,
  onRetry,
}: {
  error: ApiErrorEnvelope;
  onRetry: () => void;
}) {
  return (
    <PhoneShell>
      <ErrorState message={errorMessage(error)} onRetry={onRetry} />
    </PhoneShell>
  );
}

export function ScreenBody({
  children,
  footer,
}: {
  children: ReactNode;
  footer?: ReactNode;
}) {
  return (
    <PhoneShell>
      <div className="flex flex-1 flex-col">
        <div className="flex-1">{children}</div>
        {footer}
      </div>
    </PhoneShell>
  );
}
