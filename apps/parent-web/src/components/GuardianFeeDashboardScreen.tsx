"use client";

import { Amount, Button, StatusPill } from "@fee/ui";
import { formatPkr } from "@fee/money";
import { ChevronRight, FileText, History } from "lucide-react";
import { useRouter } from "next/navigation";
import { useCallback, useEffect, useState, type ReactNode } from "react";
import { StickyActionBar } from "@/components/PhoneShell";
import {
  ScreenBody,
  ScreenEmpty,
  ScreenError,
  ScreenLoading,
} from "@/components/ScreenStates";
import { formatDueDate, invoiceDisplayKind } from "@/lib/format";
import { clearParentSession, fetchGuardianDashboard } from "@/lib/parent/client";
import type { GuardianFeeDashboard } from "@/lib/parent/types";
import type { LoadState } from "@/lib/types";

export function GuardianFeeDashboardScreen() {
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
    if (state.error.error.code === "UNAUTHENTICATED") {
      clearParentSession();
      router.replace("/login");
      return <ScreenLoading rows={3} />;
    }
    return <ScreenError error={state.error} onRetry={() => void load()} />;
  }
  if (state.status === "empty") {
    return (
      <ScreenEmpty
        title="No students are linked to this number yet. Ask your child for a link code, or contact the university finance office."
        actionLabel="Back to login"
        onAction={() => {
          clearParentSession();
          router.push("/login");
        }}
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
        title="Home"
        subtitle={fee.institutionName}
      >
        <div className="px-[var(--space-16)] pt-[var(--space-20)] lg:px-[var(--space-24)]">
          <p className="font-[var(--font-body)] text-ink-secondary">
            Assalam-o-alaikum,{" "}
            <span className="font-[var(--font-body-strong)] text-ink">
              {fee.guardianDisplayName}
            </span>
          </p>

          <section
            className="fee-amount-hero fee-enter-delay-1 mt-[var(--space-16)] p-[var(--space-20)] ps-[calc(var(--space-20)+4px)]"
            style={{ ["--tenant-brand" as string]: fee.tenantBrandCss }}
          >
            <div className="flex items-start justify-between gap-[var(--space-12)]">
              <div>
                <p className="font-[var(--font-label)] text-ink-secondary">
                  Amount due
                </p>
                <p className="mt-[var(--space-8)] fee-display-amount tabular-nums text-ink">
                  <Amount>{formatPkr(fee.feeAmount)}</Amount>
                </p>
              </div>
              <StatusPill kind={kind} />
            </div>
            <p className="mt-[var(--space-8)] font-[var(--font-body)] text-ink-secondary">
              Due{" "}
              <bdi className="font-[var(--font-body-strong)] text-ink">
                {formatDueDate(fee.dueDate)}
              </bdi>
            </p>
            <div className="mt-[var(--space-16)] flex items-center gap-[var(--space-8)] border-t border-border pt-[var(--space-16)]">
              <span
                className="fee-tenant-chip shrink-0"
                style={{ borderColor: "var(--tenant-brand)" }}
                aria-hidden
              />
              <div className="min-w-0">
                <p className="truncate font-[var(--font-body-strong)] text-ink">
                  {fee.studentName}
                </p>
                <p className="truncate font-[var(--font-micro)] text-ink-tertiary">
                  {fee.studentExternalId} · {fee.classLabel}
                </p>
              </div>
            </div>
          </section>

          <nav
            aria-label="Fee options"
            className="fee-enter-delay-2 mt-[var(--space-16)] grid grid-cols-1 gap-[var(--space-12)] sm:grid-cols-2"
          >
            <QuickOption
              icon={<FileText size={20} aria-hidden />}
              title="Fee voucher"
              subtitle="Official challan details"
              onClick={() => router.push("/vouchers")}
            />
            <QuickOption
              icon={<History size={20} aria-hidden />}
              title="Payment history"
              subtitle="Receipts & attempts"
              onClick={() => router.push("/payments/history")}
            />
          </nav>
        </div>
      </ScreenBody>

      {fee.canPay ? (
        <StickyActionBar>
          <Button
            variant="primary"
            className="w-full"
            onClick={() => router.push(`/pay/${fee.studentId}`)}
          >
            Pay Now · <Amount>{formatPkr(fee.feeAmount)}</Amount>
          </Button>
        </StickyActionBar>
      ) : (
        <StickyActionBar>
          <Button
            variant="secondary"
            className="w-full"
            onClick={() => void load()}
          >
            Refresh status
          </Button>
        </StickyActionBar>
      )}
    </>
  );
}

function QuickOption({
  icon,
  title,
  subtitle,
  onClick,
}: {
  icon: ReactNode;
  title: string;
  subtitle: string;
  onClick: () => void;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className="fee-nav-tile flex min-h-[88px] items-center gap-[var(--space-16)] p-[var(--space-16)] text-start touch-manipulation"
    >
      <span className="flex size-11 shrink-0 items-center justify-center rounded-fee-12 bg-surface-sunken text-ink">
        {icon}
      </span>
      <span className="min-w-0 flex-1">
        <span className="block font-[var(--font-body-strong)] text-ink">
          {title}
        </span>
        <span className="mt-[var(--space-2)] block font-[var(--font-micro)] text-ink-tertiary">
          {subtitle}
        </span>
      </span>
      <ChevronRight
        size={18}
        className="shrink-0 text-ink-tertiary"
        aria-hidden
      />
    </button>
  );
}
