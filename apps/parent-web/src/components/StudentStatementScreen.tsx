"use client";

import { Amount, Button, StatusPill } from "@fee/ui";
import { formatPkr } from "@fee/money";
import { ChevronDown, ChevronUp } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { DiffHint } from "@/components/HomeFamilyScreen";
import { MockBanner, StickyActionBar } from "@/components/PhoneShell";
import {
  ScreenBody,
  ScreenEmpty,
  ScreenError,
  ScreenLoading,
} from "@/components/ScreenStates";
import {
  clearancePillKind,
  formatDueDate,
  invoiceDisplayKind,
} from "@/lib/format";
import {
  fetchStudentStatement,
  setMockStatementMode,
  type MockMode,
} from "@/lib/mock";
import type { LoadState, StatementLine, StudentStatement } from "@/lib/types";

function LineItemRow({ line }: { line: StatementLine }) {
  const [open, setOpen] = useState(false);
  const isDiscount = line.kind === "Discount" || line.amount.startsWith("-");
  const hasTrace = Boolean(line.trace);

  return (
    <li className="border-b border-border last:border-b-0">
      <button
        type="button"
        disabled={!hasTrace}
        onClick={() => setOpen((v) => !v)}
        className="flex min-h-11 w-full items-center justify-between gap-[var(--space-12)] py-[var(--space-12)] text-start touch-manipulation disabled:cursor-default"
        aria-expanded={hasTrace ? open : undefined}
      >
        <span className="font-[var(--font-body)] text-ink">{line.feeHeadName}</span>
        <span className="flex items-center gap-[var(--space-8)]">
          <bdi
            className={`font-[var(--font-body-strong)] tabular-nums ${
              isDiscount ? "text-success" : "text-ink"
            }`}
          >
            <Amount>{formatPkr(line.amount)}</Amount>
          </bdi>
          {hasTrace ? (
            open ? (
              <ChevronUp size={18} className="text-ink-tertiary" aria-hidden="true" />
            ) : (
              <ChevronDown size={18} className="text-ink-tertiary" aria-hidden="true" />
            )
          ) : null}
        </span>
      </button>
      {open && line.trace ? (
        <div className="mb-[var(--space-12)] rounded-fee-8 bg-surface-sunken px-[var(--space-12)] py-[var(--space-8)] font-[var(--font-label)] text-ink-secondary">
          {line.trace}
        </div>
      ) : null}
    </li>
  );
}

export function StudentStatementScreen({ studentId }: { studentId: string }) {
  const router = useRouter();
  const [state, setState] = useState<LoadState<StudentStatement>>({
    status: "loading",
  });

  const load = useCallback(async () => {
    setState({ status: "loading" });
    const res = await fetchStudentStatement(studentId);
    setState(res);
  }, [studentId]);

  useEffect(() => {
    void load();
  }, [load]);

  useEffect(() => {
    if (typeof window === "undefined") return;
    const params = new URLSearchParams(window.location.search);
    const mode = params.get("mock") as MockMode | null;
    if (mode === "empty" || mode === "error" || mode === "success") {
      setMockStatementMode(mode);
      void load();
    }
  }, [load]);

  if (state.status === "loading") return <ScreenLoading rows={6} />;
  if (state.status === "error") {
    return <ScreenError error={state.error} onRetry={() => void load()} />;
  }
  if (state.status === "empty") {
    return (
      <ScreenEmpty
        title="No statement for this student yet."
        actionLabel="Back to home"
        onAction={() => router.push("/home")}
      />
    );
  }

  const data = state.data;
  const visibleLines = data.lines.filter((l) => l.amount !== "0.00");
  const invoiceKind = invoiceDisplayKind({
    status: data.invoiceStatus,
    isOverdue: data.isOverdue,
    dueDate: data.dueDate,
  });
  const hasPayable = data.nextPayableAmount !== "0.00";

  return (
    <ScreenBody
      footer={
        <StickyActionBar>
          <Button
            type="button"
            className="w-full"
            disabled={!hasPayable}
            onClick={() => {
              if (hasPayable) router.push(`/pay/${studentId}`);
            }}
          >
            {hasPayable ? (
              <>
                Pay <Amount>{formatPkr(data.nextPayableAmount)}</Amount>
              </>
            ) : (
              "Paid in full"
            )}
          </Button>
          <p className="mt-[var(--space-8)] text-center font-[var(--font-micro)] text-ink-tertiary">
            {hasPayable
              ? "No card number is entered in this app."
              : "Nothing left to pay for this term."}
          </p>
        </StickyActionBar>
      }
    >
      <MockBanner />
      <div className="px-[var(--space-16)] pb-[var(--space-24)] pt-[var(--space-16)]">
        <Link
          href="/home"
          className="inline-flex min-h-11 items-center font-[var(--font-body-strong)] text-accent touch-manipulation"
        >
          <span className="rtl:hidden">← </span>
          <span className="hidden rtl:inline">→ </span>
          Home
        </Link>

        <header
          className="fee-tenant-band mt-[var(--space-12)] rounded-fee-16 bg-surface p-[var(--space-16)] shadow-fee-1"
          style={{ ["--tenant-brand" as string]: data.tenantBrandCss }}
        >
          <p className="font-[var(--font-label)] text-ink-secondary">
            {data.institutionName}
          </p>
          <h1 className="mt-[var(--space-4)] font-[var(--font-title-1)] text-ink">
            {data.studentName}
          </h1>
          <p className="mt-[var(--space-4)] font-[var(--font-label)] text-ink-secondary">
            {data.programLabel} · {data.termLabel}
          </p>
          <div className="mt-[var(--space-12)] flex flex-wrap gap-[var(--space-8)]">
            <StatusPill kind={invoiceKind} />
            <StatusPill kind={clearancePillKind(data.clearanceStatus)} />
          </div>
        </header>

        {data.updatedBannerDate ? (
          <div
            role="status"
            className="mt-[var(--space-16)] rounded-fee-12 bg-accent-soft px-[var(--space-12)] py-[var(--space-12)] font-[var(--font-body)] text-accent"
          >
            This statement was updated on{" "}
            <bdi>{formatDueDate(data.updatedBannerDate)}</bdi>. Here&apos;s what
            changed.
          </div>
        ) : null}

        <section className="mt-[var(--space-20)] text-center">
          <p className="font-[var(--font-label)] text-ink-secondary">Amount due</p>
          <p className="mt-[var(--space-8)] fee-display-amount tabular-nums text-ink">
            <Amount>
              <bdi>{formatPkr(data.amountDue)}</bdi>
            </Amount>
          </p>
          <p className="mt-[var(--space-8)] font-[var(--font-body)] text-ink-secondary">
            Due <bdi>{formatDueDate(data.dueDate)}</bdi>
          </p>
        </section>

        {data.termDiff.lines.length > 0 ? (
          <section className="mt-[var(--space-24)] rounded-fee-16 bg-surface p-[var(--space-16)] shadow-fee-1">
            <h2 className="font-[var(--font-title-2)] text-ink">
              What changed since last term
            </h2>
            <p className="mt-[var(--space-4)] font-[var(--font-label)] text-ink-secondary">
              Total {data.termDiff.totalPercentChange > 0 ? "+" : ""}
              {data.termDiff.totalPercentChange.toFixed(1)}%
            </p>
            <ul className="mt-[var(--space-12)] flex flex-col gap-[var(--space-8)]">
              {data.termDiff.lines.map((line) => (
                <DiffHint
                  key={line.feeHeadName}
                  direction={line.direction}
                  percent={line.percentChange}
                  name={line.feeHeadName}
                />
              ))}
            </ul>
          </section>
        ) : null}

        <section className="mt-[var(--space-24)] rounded-fee-16 bg-surface px-[var(--space-16)] shadow-fee-1">
          <h2 className="pt-[var(--space-16)] font-[var(--font-title-2)] text-ink">
            Statement lines
          </h2>
          <ul className="mt-[var(--space-8)]">
            {visibleLines.map((line) => (
              <LineItemRow key={line.id} line={line} />
            ))}
          </ul>
          <div className="flex items-center justify-between border-t border-border-strong py-[var(--space-16)]">
            <span className="font-[var(--font-body-strong)] text-ink">Total due</span>
            <bdi className="font-[var(--font-title-2)] tabular-nums text-ink">
              <Amount>{formatPkr(data.amountDue)}</Amount>
            </bdi>
          </div>
        </section>

        {data.instalments.length > 0 ? (
          <section className="mt-[var(--space-24)]">
            <h2 className="font-[var(--font-title-2)] text-ink">Instalments</h2>
            <ul className="mt-[var(--space-12)] flex flex-col gap-[var(--space-12)]">
              {data.instalments.map((inst) => (
                <li
                  key={inst.id}
                  className="rounded-fee-12 border border-border bg-surface px-[var(--space-16)] py-[var(--space-12)]"
                >
                  <div className="flex items-center justify-between gap-[var(--space-8)]">
                    <p className="font-[var(--font-body-strong)] text-ink">
                      Instalment {inst.sequence}
                    </p>
                    <StatusPill
                      kind={invoiceDisplayKind({
                        status: inst.status,
                        isOverdue: inst.isOverdue,
                        dueDate: inst.dueDate,
                      })}
                    />
                  </div>
                  <p className="mt-[var(--space-4)] font-[var(--font-label)] text-ink-secondary">
                    Due <bdi>{formatDueDate(inst.dueDate)}</bdi>
                  </p>
                  <p className="mt-[var(--space-8)] font-[var(--font-title-2)] tabular-nums text-ink">
                    <Amount>{formatPkr(inst.amount)}</Amount>
                  </p>
                </li>
              ))}
            </ul>
          </section>
        ) : null}

        <div className="mt-[var(--space-24)] flex flex-col gap-[var(--space-8)]">
          <Button type="button" variant="ghost" className="w-full" disabled>
            Download statement (PDF)
          </Button>
          <Button type="button" variant="ghost" className="w-full" disabled>
            Request more time
          </Button>
          <p className="text-center font-[var(--font-micro)] text-ink-tertiary">
            PDF and deferment connect when parent APIs land.
          </p>
        </div>
      </div>
    </ScreenBody>
  );
}
