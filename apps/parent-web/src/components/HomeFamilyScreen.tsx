"use client";

import { Amount, Button, StatusPill } from "@fee/ui";
import { formatPkr } from "@fee/money";
import { ArrowDown, ArrowUp, ChevronRight } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { MockBanner, StickyActionBar } from "@/components/PhoneShell";
import {
  ScreenBody,
  ScreenEmpty,
  ScreenError,
  ScreenLoading,
} from "@/components/ScreenStates";
import { formatDueDate, invoiceDisplayKind } from "@/lib/format";
import { fetchFamilyHome, setMockHomeMode, type MockMode } from "@/lib/mock";
import type { FamilyHome, LinkedStudent, LoadState } from "@/lib/types";

function StudentCard({ student }: { student: LinkedStudent }) {
  const kind = invoiceDisplayKind({
    status: student.invoiceStatus,
    isOverdue: student.isOverdue,
    dueDate: student.dueDate,
  });

  return (
    <Link
      href={`/students/${student.id}/statement`}
      className="fee-tenant-band block rounded-fee-16 bg-surface p-[var(--space-16)] shadow-fee-1 transition-[transform] duration-fee-press ease-fee-out active:scale-[0.98] touch-manipulation"
      style={{ ["--tenant-brand" as string]: student.tenantBrandCss }}
    >
      <div className="flex items-start gap-[var(--space-12)]">
        <span
          className="fee-tenant-chip shrink-0"
          style={{ borderColor: "var(--tenant-brand)" }}
          aria-hidden="true"
        />
        <div className="min-w-0 flex-1">
          <div className="flex items-start justify-between gap-[var(--space-8)]">
            <p className="font-[var(--font-title-2)] text-ink">{student.fullName}</p>
            <ChevronRight
              size={20}
              className="mt-1 shrink-0 text-ink-tertiary rtl:rotate-180"
              aria-hidden="true"
            />
          </div>
          <p className="mt-[var(--space-4)] font-[var(--font-label)] text-ink-secondary">
            {student.programLabel} · {student.termLabel}
          </p>
          <div className="mt-[var(--space-8)]">
            <StatusPill kind={kind} />
          </div>
          <p className="mt-[var(--space-12)] font-[var(--font-title-1)] tabular-nums text-ink">
            <Amount>{formatPkr(student.amountDue)}</Amount>
          </p>
          <p className="mt-[var(--space-4)] font-[var(--font-label)] text-ink-secondary">
            Due <bdi>{formatDueDate(student.dueDate)}</bdi>
          </p>
        </div>
      </div>
    </Link>
  );
}

export function HomeFamilyScreen() {
  const router = useRouter();
  const [state, setState] = useState<LoadState<FamilyHome>>({ status: "loading" });

  const load = useCallback(async () => {
    setState({ status: "loading" });
    const res = await fetchFamilyHome();
    setState(res);
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  useEffect(() => {
    if (typeof window === "undefined") return;
    const params = new URLSearchParams(window.location.search);
    const mode = params.get("mock") as MockMode | null;
    if (mode === "empty" || mode === "error" || mode === "allPaid" || mode === "success") {
      setMockHomeMode(mode);
      void load();
    }
  }, [load]);

  if (state.status === "loading") return <ScreenLoading rows={5} />;
  if (state.status === "error") {
    return <ScreenError error={state.error} onRetry={() => void load()} />;
  }
  if (state.status === "empty") {
    return (
      <ScreenEmpty
        title="No students are linked to this number yet. Ask your child for a link code from their student account, or contact the university finance office."
        actionLabel="Back to login"
        onAction={() => router.push("/login")}
      />
    );
  }

  const data = state.data;
  const hasDue = data.nextDue && data.totalDue !== "0.00";

  return (
    <ScreenBody
      footer={
        hasDue && data.nextDue ? (
          <StickyActionBar>
            <Button
              type="button"
              className="w-full"
              onClick={() =>
                router.push(`/pay/${data.nextDue!.studentId}`)
              }
            >
              Pay next due · <Amount>{formatPkr(data.nextDue.amount)}</Amount>
            </Button>
            <p className="mt-[var(--space-8)] text-center font-[var(--font-micro)] text-ink-tertiary">
              No card number is entered in this app.
            </p>
          </StickyActionBar>
        ) : (
          <StickyActionBar>
            <Button type="button" className="w-full" disabled>
              All clear · nothing due
            </Button>
          </StickyActionBar>
        )
      }
    >
      <MockBanner />
      <div className="px-[var(--space-16)] pb-[var(--space-24)] pt-[var(--space-20)]">
        <div className="flex items-center justify-between gap-[var(--space-12)]">
          <p className="font-[var(--font-body)] text-ink-secondary">
            Assalam-o-Alaikum, {data.parent.displayName}
          </p>
          <span
            className="min-h-11 min-w-11 inline-flex items-center justify-center font-[var(--font-label)] text-ink-tertiary"
            aria-label="Language: English"
          >
            EN
          </span>
        </div>

        {data.firstVisitLinkedCount ? (
          <p
            role="status"
            className="mt-[var(--space-12)] rounded-fee-12 bg-accent-soft px-[var(--space-12)] py-[var(--space-12)] font-[var(--font-body)] text-accent"
          >
            We found {data.firstVisitLinkedCount} students linked to your number.
          </p>
        ) : null}

        <section className="mt-[var(--space-20)] rounded-fee-16 bg-surface p-[var(--space-20)] shadow-fee-1">
          <p className="font-[var(--font-label)] text-ink-secondary">
            Total due across your family
          </p>
          <p className="mt-[var(--space-8)] fee-display-amount tabular-nums text-ink">
            <Amount>{formatPkr(data.totalDue)}</Amount>
          </p>
          {data.nextDue ? (
            <p className="mt-[var(--space-12)] font-[var(--font-body)] text-ink-secondary">
              Next due:{" "}
              <bdi>{formatDueDate(data.nextDue.dueDate)}</bdi>
              {" · "}
              {data.nextDue.studentName.split(" ")[0]}
              {" · "}
              <bdi>
                <Amount>{formatPkr(data.nextDue.amount)}</Amount>
              </bdi>
            </p>
          ) : (
            <p className="mt-[var(--space-12)] font-[var(--font-body)] text-success">
              No fees due. You&apos;re all set for this term.
            </p>
          )}
        </section>

        {data.clearanceAlerts.map((alert) => (
          <div
            key={alert.studentId}
            role="status"
            className="mt-[var(--space-16)] rounded-fee-12 border border-warning bg-warning-soft px-[var(--space-12)] py-[var(--space-12)]"
          >
            <p className="font-[var(--font-body-strong)] text-warning">
              {alert.studentName}&apos;s exam clearance needs{" "}
              <bdi>
                <Amount>{formatPkr(alert.amountDue)}</Amount>
              </bdi>{" "}
              by <bdi>{formatDueDate(alert.cutoffDate)}</bdi>.
            </p>
          </div>
        ))}

        <div className="mt-[var(--space-24)] flex flex-col gap-[var(--space-24)]">
          {data.groups.map((group) => (
            <section key={group.institutionId}>
              <div className="mb-[var(--space-12)] flex items-center gap-[var(--space-12)]">
                <span
                  className="fee-tenant-chip"
                  style={{ ["--tenant-brand" as string]: group.tenantBrandCss }}
                  aria-hidden="true"
                />
                <h2 className="font-[var(--font-title-2)] text-ink">
                  {group.institutionName}
                </h2>
              </div>
              <ul className="flex flex-col gap-[var(--space-12)]">
                {group.students.map((student) => (
                  <li key={student.id}>
                    <StudentCard student={student} />
                  </li>
                ))}
              </ul>
            </section>
          ))}
        </div>

        {!hasDue ? (
          <p className="mt-[var(--space-24)] text-center font-[var(--font-body)] text-ink-secondary">
            No fees due. You&apos;re all set.
          </p>
        ) : null}
      </div>
    </ScreenBody>
  );
}

/** Quiet term-diff arrows with words (DESIGN §6.3). */
export function DiffHint({
  direction,
  percent,
  name,
}: {
  direction: "up" | "down" | "flat";
  percent: number;
  name: string;
}) {
  const Icon = direction === "up" ? ArrowUp : direction === "down" ? ArrowDown : null;
  const word =
    direction === "up" ? "up" : direction === "down" ? "down" : "unchanged";
  return (
    <li className="flex items-center gap-[var(--space-8)] font-[var(--font-label)] text-ink-secondary">
      {Icon ? <Icon size={16} aria-hidden="true" /> : null}
      <span>
        {name} {percent > 0 ? "+" : ""}
        {percent.toFixed(1)}% ({word})
      </span>
    </li>
  );
}
