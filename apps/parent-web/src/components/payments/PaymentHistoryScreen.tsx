"use client";

import { Amount, StatusPill } from "@fee/ui";
import { formatPkr } from "@fee/money";
import { ChevronRight } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import {
  ScreenBody,
  ScreenEmpty,
  ScreenError,
  ScreenLoading,
} from "@/components/ScreenStates";
import { formatPaidAt } from "@/lib/format";
import {
  fetchGuardianDashboard,
  fetchParentPaymentHistory,
  type ParentPaymentHistoryItem,
} from "@/lib/parent/client";
import type { LoadState } from "@/lib/types";

type HistoryGroup = {
  termLabel: string;
  items: ParentPaymentHistoryItem[];
};

function groupHistory(items: ParentPaymentHistoryItem[]): HistoryGroup[] {
  const byTerm = new Map<string, ParentPaymentHistoryItem[]>();
  for (const item of items) {
    const list = byTerm.get(item.termLabel) ?? [];
    list.push(item);
    byTerm.set(item.termLabel, list);
  }
  return [...byTerm.entries()].map(([termLabel, termItems]) => ({
    termLabel,
    items: termItems,
  }));
}

export function PaymentHistoryScreen() {
  const router = useRouter();
  const [state, setState] = useState<LoadState<HistoryGroup[]>>({
    status: "loading",
  });
  const [studentLabel, setStudentLabel] = useState<string | null>(null);

  const load = useCallback(async () => {
    setState({ status: "loading" });
    const dash = await fetchGuardianDashboard();
    if (dash.status === "error") {
      setState({ status: "error", error: dash.error });
      return;
    }
    if (dash.status === "empty") {
      setStudentLabel(null);
      setState({ status: "empty" });
      return;
    }

    setStudentLabel(dash.data.studentName);
    const history = await fetchParentPaymentHistory(dash.data.studentId);
    if (history.status === "error") {
      setState({ status: "error", error: history.error });
      return;
    }
    if (history.status === "empty") {
      setState({ status: "empty" });
      return;
    }
    setState({ status: "success", data: groupHistory(history.data) });
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  if (state.status === "loading") return <ScreenLoading rows={5} />;
  if (state.status === "error") {
    return <ScreenError error={state.error} onRetry={() => void load()} />;
  }
  if (state.status === "empty") {
    return (
      <ScreenEmpty
        title={
          studentLabel
            ? `No payments yet for ${studentLabel}. Creating a voucher only opens the fee — use Pay Now, then it appears here.`
            : "No payments yet. Creating a voucher only opens the fee — use Pay Now, then it appears here."
        }
        actionLabel="Back to home"
        onAction={() => router.push("/home")}
      />
    );
  }

  return (
    <ScreenBody
      title="Payment history"
      subtitle={studentLabel ? `For ${studentLabel}` : undefined}
    >
      <div className="px-[var(--space-16)] pb-[var(--space-24)] pt-[var(--space-16)] lg:px-[var(--space-24)]">
        <div className="flex flex-col gap-[var(--space-24)]">
          {state.data.map((group) => (
            <section key={group.termLabel} className="fee-enter-delay-1">
              <h2 className="fee-title-2 text-ink">{group.termLabel}</h2>
              <ul className="mt-[var(--space-12)] flex flex-col gap-[var(--space-12)]">
                {group.items.map((item) => {
                  const href = item.receiptId
                    ? `/payments/receipts/${item.receiptId}`
                    : `/payments/attempts/${item.attemptId}`;
                  return (
                    <li key={item.attemptId}>
                      <Link
                        href={href}
                        className="fee-nav-tile flex min-h-11 items-center justify-between gap-[var(--space-12)] p-[var(--space-16)] touch-manipulation"
                      >
                        <div className="min-w-0">
                          <p className="font-[var(--font-title-2)] tabular-nums text-ink">
                            <Amount>{formatPkr(item.amount)}</Amount>
                          </p>
                          <p className="mt-[var(--space-4)] font-[var(--font-label)] text-ink-secondary">
                            {item.channelName}
                          </p>
                          <p className="mt-[var(--space-4)] font-[var(--font-label)] text-ink-tertiary">
                            <bdi>{formatPaidAt(item.paidAtUtc)}</bdi>
                          </p>
                          <div className="mt-[var(--space-8)]">
                            <StatusPill kind={item.status} />
                          </div>
                        </div>
                        <ChevronRight
                          size={20}
                          className="shrink-0 text-ink-tertiary rtl:rotate-180"
                          aria-hidden="true"
                        />
                      </Link>
                    </li>
                  );
                })}
              </ul>
            </section>
          ))}
        </div>
      </div>
    </ScreenBody>
  );
}
