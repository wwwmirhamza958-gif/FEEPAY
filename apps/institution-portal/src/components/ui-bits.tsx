import { clsx } from "clsx";
import type { InputHTMLAttributes, ReactNode } from "react";

export function PageHeader({
  title,
  description,
  primaryAction,
}: {
  title: string;
  description?: string;
  primaryAction?: ReactNode;
}) {
  return (
    <header className="mb-[var(--space-24)] flex flex-wrap items-start justify-between gap-[var(--space-16)]">
      <div className="min-w-0">
        <h1 className="text-ink">{title}</h1>
        {description ? (
          <p className="mt-[var(--space-4)] max-w-2xl text-[15px] leading-[22px] text-ink-secondary">
            {description}
          </p>
        ) : null}
      </div>
      {primaryAction ? (
        <div className="shrink-0">{primaryAction}</div>
      ) : null}
    </header>
  );
}

export function Panel({
  children,
  className,
}: {
  children: ReactNode;
  className?: string;
}) {
  return (
    <div
      className={clsx(
        "rounded-fee-16 border border-border bg-surface p-[var(--space-16)] shadow-fee-1",
        className
      )}
    >
      {children}
    </div>
  );
}

export function FieldLabel({
  htmlFor,
  children,
}: {
  htmlFor?: string;
  children: ReactNode;
}) {
  return (
    <label
      htmlFor={htmlFor}
      className="mb-[var(--space-4)] block text-[14px] font-medium text-ink"
    >
      {children}
    </label>
  );
}

export function TextInput({
  id,
  className,
  ...props
}: InputHTMLAttributes<HTMLInputElement>) {
  return (
    <input
      id={id}
      className={clsx(
        "w-full rounded-fee-8 border border-border-strong bg-surface px-[var(--space-12)] py-[var(--space-8)] text-[15px] text-ink placeholder:text-ink-tertiary focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent disabled:opacity-50",
        className
      )}
      {...props}
    />
  );
}

export function TableShell({
  children,
  caption,
}: {
  children: ReactNode;
  caption: string;
}) {
  return (
    <div className="overflow-x-auto rounded-fee-12 border border-border">
      <table className="w-full min-w-[640px] border-collapse text-start text-[15px]">
        <caption className="sr-only">{caption}</caption>
        {children}
      </table>
    </div>
  );
}

export function Th({
  children,
  align = "start",
  ariaSort,
}: {
  children: ReactNode;
  align?: "start" | "end";
  ariaSort?: "none" | "ascending" | "descending" | "other";
}) {
  return (
    <th
      scope="col"
      aria-sort={ariaSort}
      className={clsx(
        "sticky top-0 bg-surface-sunken px-[var(--space-12)] py-[var(--space-8)] text-[14px] font-medium text-ink-secondary",
        align === "end" ? "text-end" : "text-start"
      )}
    >
      {children}
    </th>
  );
}

export function Td({
  children,
  align = "start",
  className,
}: {
  children: ReactNode;
  align?: "start" | "end";
  className?: string;
}) {
  return (
    <td
      className={clsx(
        "fee-table-row border-t border-border px-[var(--space-12)] py-[var(--space-8)] align-middle text-ink",
        align === "end" ? "text-end tabular-nums" : "text-start",
        className
      )}
    >
      {children}
    </td>
  );
}

export function TableSkeleton({ rows = 5, cols = 4 }: { rows?: number; cols?: number }) {
  return (
    <div className="space-y-[var(--space-8)]" aria-busy="true" aria-label="Loading">
      {Array.from({ length: rows }).map((_, i) => (
        <div
          key={i}
          className="flex gap-[var(--space-12)]"
          style={{ minBlockSize: 44 }}
        >
          {Array.from({ length: cols }).map((__, j) => (
            <span
              key={j}
              className="inline-block flex-1 animate-pulse rounded-fee-8 bg-surface-sunken"
              style={{ minBlockSize: 16 }}
              aria-hidden="true"
            />
          ))}
        </div>
      ))}
    </div>
  );
}

export function MockBanner() {
  const mock = process.env.NEXT_PUBLIC_PORTAL_DATA === "mock";
  return (
    <p
      role="note"
      className="mb-[var(--space-16)] rounded-fee-8 border border-border bg-surface-sunken px-[var(--space-12)] py-[var(--space-8)] text-[14px] text-ink-secondary"
    >
      {mock
        ? "Using local mock data (NEXT_PUBLIC_PORTAL_DATA=mock)."
        : "Live P1 API via /api/platform rewrite → NEXT_PUBLIC_API_BASE_URL. Fee-schedule / discount writes may return 501."}
    </p>
  );
}
