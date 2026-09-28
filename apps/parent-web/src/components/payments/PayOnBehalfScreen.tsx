"use client";

import { Amount, Button } from "@fee/ui";
import { formatPkr } from "@fee/money";
import { useRouter } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { MockBanner, StickyActionBar } from "@/components/PhoneShell";
import {
  ScreenBody,
  ScreenError,
  ScreenLoading,
} from "@/components/ScreenStates";
import { formatDueDate } from "@/lib/format";
import {
  createPaymentAttempt,
  fetchPayOnBehalf,
  pickRecommendedChannel,
  type PayOnBehalfPreview,
} from "@/lib/payments";
import { mockChannels } from "@/lib/payments/mock-data";
import type { LoadState } from "@/lib/types";

/**
 * No-account pay-on-behalf (§6.9): institution, initials, term, amount, due date only.
 * Same hand-off rules as Pay — no login, no card field, no WebView.
 */
export function PayOnBehalfScreen({ token }: { token: string }) {
  const router = useRouter();
  const [state, setState] = useState<LoadState<PayOnBehalfPreview>>({
    status: "loading",
  });
  const [busy, setBusy] = useState(false);
  const [handoff, setHandoff] = useState<string | null>(null);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  const load = useCallback(async () => {
    setState({ status: "loading" });
    setState(await fetchPayOnBehalf(token));
  }, [token]);

  useEffect(() => {
    void load();
  }, [load]);

  const onPay = async (data: PayOnBehalfPreview) => {
    setErrorMsg(null);
    setBusy(true);
    const channel = pickRecommendedChannel(mockChannels);
    if (!channel) {
      setBusy(false);
      setErrorMsg("No payment methods are available right now.");
      return;
    }
    setHandoff(channel.handoffLabel);

    // demo-hamza maps to stu-hamza; other tokens encode student id after pob-
    const studentId = token.startsWith("pob-")
      ? token.split("-").slice(1, 3).join("-") || "stu-hamza"
      : "stu-hamza";

    const result = await createPaymentAttempt({
      studentId,
      amount: data.amount,
      selection: "instalment",
      channelId: channel.id,
      idempotencyKey:
        typeof crypto !== "undefined" && "randomUUID" in crypto
          ? crypto.randomUUID()
          : `idem-pob-${Date.now()}`,
    });

    if (!result.ok) {
      setBusy(false);
      setHandoff(null);
      setErrorMsg(result.error.error.message);
      return;
    }

    await new Promise((r) => setTimeout(r, 900));
    if (result.data.redirectUrl) {
      window.open(result.data.redirectUrl, "_blank", "noopener,noreferrer");
    }
    setBusy(false);
    router.push(`/payments/attempts/${result.data.id}`);
  };

  if (state.status === "loading") return <ScreenLoading rows={4} />;
  if (state.status === "error") {
    return <ScreenError error={state.error} onRetry={() => void load()} />;
  }
  if (state.status !== "success") {
    return (
      <ScreenError
        error={{
          error: {
            code: "RESOURCE_NOT_FOUND",
            message: "This pay link is invalid or has expired.",
            messageKey: "errors.resource_not_found",
            correlationId: "f0000103-aaaa-bbbb-cccc-dddddddddddd",
            details: [],
            retryable: false,
          },
        }}
        onRetry={() => void load()}
      />
    );
  }

  const data = state.data;

  if (handoff) {
    return (
      <ScreenBody>
        <div className="flex flex-1 flex-col items-center justify-center gap-[var(--space-16)] px-[var(--space-24)] text-center">
          <div className="fee-pending-ring" aria-hidden="true" />
          <p className="font-[var(--font-title-1)] text-ink">{handoff}</p>
          <p className="font-[var(--font-body)] text-ink-secondary">
            When you&apos;re done, you&apos;ll come right back here.
          </p>
        </div>
      </ScreenBody>
    );
  }

  return (
    <ScreenBody
      footer={
        <StickyActionBar>
          {errorMsg ? (
            <p
              role="alert"
              className="mb-[var(--space-8)] text-center font-[var(--font-body)] text-danger"
            >
              {errorMsg}
            </p>
          ) : null}
          <Button
            type="button"
            className="w-full"
            loading={busy}
            onClick={() => void onPay(data)}
          >
            Pay <Amount>{formatPkr(data.amount)}</Amount>
          </Button>
          <p className="mt-[var(--space-8)] text-center font-[var(--font-micro)] text-ink-tertiary">
            Paid directly to {data.institutionName} · No account needed
          </p>
        </StickyActionBar>
      }
    >
      <MockBanner />
      <div className="px-[var(--space-16)] pb-[var(--space-24)] pt-[var(--space-24)]">
        <div
          className="fee-tenant-band rounded-fee-16 bg-surface p-[var(--space-20)] shadow-fee-1"
          style={{ ["--tenant-brand" as string]: data.tenantBrandCss }}
        >
          <p className="font-[var(--font-label)] text-ink-secondary">
            {data.institutionName}
          </p>
          <p className="mt-[var(--space-16)] font-[var(--font-title-1)] text-ink">
            {data.studentInitials}
          </p>
          <p className="mt-[var(--space-4)] font-[var(--font-body)] text-ink-secondary">
            {data.termLabel}
          </p>
          <p className="mt-[var(--space-20)] fee-display-amount tabular-nums text-ink">
            <Amount>{formatPkr(data.amount)}</Amount>
          </p>
          <p className="mt-[var(--space-8)] font-[var(--font-body)] text-ink-secondary">
            Due <bdi>{formatDueDate(data.dueDate)}</bdi>
          </p>
          <p className="mt-[var(--space-16)] font-[var(--font-label)] text-ink-tertiary">
            {data.requestedByLabel}
          </p>
        </div>
        <p className="mt-[var(--space-16)] font-[var(--font-label)] text-ink-tertiary">
          No card number is collected on this page.
        </p>
      </div>
    </ScreenBody>
  );
}
