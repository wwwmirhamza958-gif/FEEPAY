"use client";

import { Amount, StatusPill } from "@fee/ui";
import { formatPkr } from "@fee/money";
import { ChevronRight } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { MockBanner } from "@/components/PhoneShell";
import {
  ScreenBody,
  ScreenEmpty,
  ScreenError,
  ScreenLoading,
} from "@/components/ScreenStates";
import { formatPaidAt } from "@/lib/format";
import {
  fetchPaymentHistory,
  type PaymentHistoryGroup,
} from "@/lib/payments";
import type { LoadState } from "@/lib/types";

export function PaymentHistoryScreen() {
  const router = useRouter();
  const [state, setState] = useState<LoadState<PaymentHistoryGroup[]>>({
    status: "loading",
  });

  const load = useCallback(async () => {
    setState({ status: "loading" });
    setState(await fetchPaymentHistory());
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
        title="No payments yet. When you pay a statement, receipts show up here."
        actionLabel="Back to home"
        onAction={() => router.push("/home")}
      />
    );
  }

  return (
    <ScreenBody>
      <MockBanner />
      <div className="px-[var(--space-16)] pb-[var(--space-24)] pt-[var(--space-16)]">
        <Link
          href="/home"
          className="inline-flex min-h-11 items-center font-[var(--font-body-strong)] text-accent touch-manipulation"
        >
          Home
        </Link>
        <h1 className="mt-[var(--space-12)] font-[var(--font-title-1)] text-ink">
          Payment history
        </h1>

        <div className="mt-[var(--space-20)] flex flex-col gap-[var(--space-24)]">
          {state.data.map((group) => (
            <section key={group.termLabel}>
              <h2 className="font-[var(--font-title-2)] text-ink">
                {group.termLabel}
              </h2>
              <ul className="mt-[var(--space-12)] flex flex-col gap-[var(--space-12)]">
                {group.items.map((item) => {
                  const href = item.receiptId
                    ? `/payments/receipts/${item.receiptId}`
                    : `/payments/attempts/${item.id}`;
                  return (
                    <li key={item.id}>
                      <Link
                        href={href}
                        className="flex min-h-11 items-center justify-between gap-[var(--space-12)] rounded-fee-16 bg-surface p-[var(--space-16)] shadow-fee-1 touch-manipulation"
                      >
                        <div className="min-w-0">
                          <p className="font-[var(--font-title-2)] tabular-nums text-ink">
                            <Amount>{formatPkr(item.amount)}</Amount>
                          </p>
                          <p className="mt-[var(--space-4)] font-[var(--font-label)] text-ink-secondary">
                            {item.studentName} · {item.channelName}
                          </p>
                          <p className="mt-[var(--space-4)] font-[var(--font-label)] text-ink-tertiary">
                            <bdi>{formatPaidAt(item.paidAt)}</bdi>
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
