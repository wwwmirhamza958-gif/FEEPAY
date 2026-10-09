"use client";

import { Amount, Button, StatusPill } from "@fee/ui";
import { formatPkr } from "@fee/money";
import { useRouter } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { StickyActionBar } from "@/components/PhoneShell";
import {
  ScreenBody,
  ScreenEmpty,
  ScreenError,
  ScreenLoading,
} from "@/components/ScreenStates";
import { formatDueDate, invoiceDisplayKind } from "@/lib/format";
import { fetchGuardianDashboard } from "@/lib/parent/client";
import type { GuardianFeeDashboard } from "@/lib/parent/types";
import type { LoadState } from "@/lib/types";

function voucherRef(invoiceId: string | null): string {
  if (!invoiceId) return "—";
  const compact = invoiceId.replace(/-/g, "").toUpperCase();
  return `FV-${compact.slice(-8)}`;
}

export function FeeVoucherScreen() {
  const router = useRouter();
  const [state, setState] = useState<LoadState<GuardianFeeDashboard>>({
    status: "loading",
  });

  const load = useCallback(async () => {
    setState({ status: "loading" });
    setState(await fetchGuardianDashboard());
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
        title="No fee voucher is available yet."
        actionLabel="Back to home"
        onAction={() => router.push("/home")}
      />
    );
  }

  const fee = state.data;
  const kind = invoiceDisplayKind({
    status: fee.paymentStatus as
      | "Issued"
      | "Paid"
      | "PartiallyPaid"
      | "Draft"
      | "Superseded",
    isOverdue: fee.isOverdue,
    dueDate: fee.dueDate,
  });

  return (
    <>
      <ScreenBody
        className="pb-28"
        title="Fee voucher"
        subtitle="Official challan"
      >
        <div className="px-[var(--space-16)] pt-[var(--space-16)] lg:px-[var(--space-24)]">
          <article
            className="fee-amount-hero fee-enter-delay-1 overflow-hidden"
            style={{ ["--tenant-brand" as string]: fee.tenantBrandCss }}
          >
            <div className="border-b border-border px-[var(--space-20)] py-[var(--space-16)] ps-[calc(var(--space-20)+4px)]">
              <div className="flex items-start justify-between gap-[var(--space-12)]">
                <div className="min-w-0">
                  <p className="font-[var(--font-label)] text-ink-secondary">
                    Institution
                  </p>
                  <p className="mt-[var(--space-4)] font-[var(--font-title-2)] text-ink">
                    {fee.institutionName}
                  </p>
                </div>
                <StatusPill kind={kind} />
              </div>
              <p className="mt-[var(--space-12)] font-[var(--font-micro)] tabular-nums text-ink-tertiary">
                Voucher {voucherRef(fee.invoiceId)}
              </p>
            </div>

            <div className="px-[var(--space-20)] py-[var(--space-20)] ps-[calc(var(--space-20)+4px)]">
              <p className="font-[var(--font-label)] text-ink-secondary">
                Amount payable
              </p>
              <p className="mt-[var(--space-8)] fee-display-amount tabular-nums text-ink">
                <Amount>{formatPkr(fee.feeAmount)}</Amount>
              </p>
              <p className="mt-[var(--space-8)] font-[var(--font-body)] text-ink-secondary">
                Due{" "}
                <bdi className="font-[var(--font-body-strong)] text-ink">
                  {formatDueDate(fee.dueDate)}
                </bdi>
              </p>
            </div>

            <dl className="divide-y divide-border border-t border-border">
              <VoucherRow label="Student" value={fee.studentName} />
              <VoucherRow label="Student ID" value={fee.studentExternalId} />
              <VoucherRow label="Class / Program" value={fee.classLabel} />
              <VoucherRow label="Guardian" value={fee.guardianDisplayName} />
            </dl>
          </article>

          <p className="mt-[var(--space-16)] font-[var(--font-micro)] text-ink-tertiary">
            Keep this voucher reference if you need help from the finance
            office.
          </p>
        </div>
      </ScreenBody>

      {fee.canPay ? (
        <StickyActionBar>
          <Button
            variant="primary"
            className="w-full"
            onClick={() => router.push(`/pay/${fee.studentId}`)}
          >
            Pay voucher · <Amount>{formatPkr(fee.feeAmount)}</Amount>
          </Button>
        </StickyActionBar>
      ) : (
        <StickyActionBar>
          <Button
            variant="secondary"
            className="w-full"
            onClick={() => router.push("/home")}
          >
            Back to home
          </Button>
        </StickyActionBar>
      )}
    </>
  );
}

function VoucherRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-start justify-between gap-[var(--space-16)] px-[var(--space-20)] py-[var(--space-12)] ps-[calc(var(--space-20)+4px)] min-h-11">
      <dt className="shrink-0 font-[var(--font-label)] text-ink-secondary">
        {label}
      </dt>
      <dd className="min-w-0 text-end font-[var(--font-body-strong)] text-ink">
        {value}
      </dd>
    </div>
  );
}
