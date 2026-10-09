"use client";

import { Button, EmptyState, ErrorState, Skeleton } from "@fee/ui";
import type { ApiErrorEnvelope } from "@fee/api-client";
import type { ReactNode } from "react";
import { ParentAppShell, PhoneShell } from "./PhoneShell";

export function errorMessage(envelope: ApiErrorEnvelope): string {
  return envelope.error.message;
}

export function ScreenLoading({
  rows = 4,
  guest = false,
}: {
  rows?: number;
  guest?: boolean;
}) {
  const body = (
    <div
      className="flex flex-1 flex-col gap-[var(--space-16)] px-[var(--space-16)] py-[var(--space-24)] fee-enter"
      aria-busy="true"
      aria-label="Loading"
    >
      <Skeleton className="h-5 w-1/3" />
      <Skeleton className="h-10 w-2/3" />
      <div className="fee-amount-hero p-[var(--space-20)]">
        <Skeleton className="h-4 w-1/4" />
        <Skeleton className="mt-[var(--space-12)] h-12 w-3/4" />
        <Skeleton className="mt-[var(--space-8)] h-4 w-1/3" />
      </div>
      {Array.from({ length: Math.max(rows - 2, 1) }).map((_, i) => (
        <Skeleton key={i} className="h-16 w-full rounded-fee-16" />
      ))}
    </div>
  );

  if (guest) return <PhoneShell>{body}</PhoneShell>;
  return <ParentAppShell title="Loading">{body}</ParentAppShell>;
}

export function ScreenEmpty({
  title,
  actionLabel,
  onAction,
  guest = false,
}: {
  title: string;
  actionLabel?: string;
  onAction?: () => void;
  guest?: boolean;
}) {
  const body = (
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
  );
  if (guest) return <PhoneShell>{body}</PhoneShell>;
  return <ParentAppShell title="Fee Platform">{body}</ParentAppShell>;
}

export function ScreenError({
  error,
  onRetry,
  guest = false,
}: {
  error: ApiErrorEnvelope;
  onRetry: () => void;
  guest?: boolean;
}) {
  const body = <ErrorState message={errorMessage(error)} onRetry={onRetry} />;
  if (guest) return <PhoneShell>{body}</PhoneShell>;
  return <ParentAppShell title="Something went wrong">{body}</ParentAppShell>;
}

export function ScreenBody({
  children,
  footer,
  className,
  title,
  subtitle,
  hideChrome,
}: {
  children: ReactNode;
  footer?: ReactNode;
  className?: string;
  title?: string;
  subtitle?: string;
  hideChrome?: boolean;
}) {
  return (
    <ParentAppShell title={title} subtitle={subtitle} hideChrome={hideChrome}>
      <div className={`flex flex-1 flex-col ${className ?? ""}`}>
        <div className="flex-1 fee-enter">{children}</div>
        {footer}
      </div>
    </ParentAppShell>
  );
}
