"use client";

import { Amount, Button } from "@fee/ui";
import { formatPkr } from "@fee/money";
import { ChevronRight, Lock, ShieldCheck } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useCallback, useEffect, useId, useMemo, useState } from "react";
import { ParentBottomSheet } from "@/components/payments/ParentBottomSheet";
import { MockBanner, StickyActionBar } from "@/components/PhoneShell";
import {
  ScreenBody,
  ScreenEmpty,
  ScreenError,
  ScreenLoading,
} from "@/components/ScreenStates";
import { formatDueDate } from "@/lib/format";
import {
  createPaymentAttempt,
  fetchPayContext,
  pickRecommendedChannel,
  sortChannelsByTier,
  type PayContext,
  type PaymentChannel,
  type PaymentSelection,
} from "@/lib/payments";
import type { LoadState } from "@/lib/types";

type Step = 1 | 2 | 3;

function ChannelRow({
  channel,
  selected,
  onSelect,
  recommended,
}: {
  channel: PaymentChannel;
  selected: boolean;
  onSelect: () => void;
  recommended?: boolean;
}) {
  return (
    <button
      type="button"
      onClick={onSelect}
      aria-pressed={selected}
      className={`w-full rounded-fee-16 border p-[var(--space-16)] text-start touch-manipulation transition-[transform,opacity] duration-fee-press ease-fee-out active:scale-[0.98] ${
        selected
          ? "border-border-strong bg-surface shadow-fee-1"
          : "border-border bg-surface"
      } ${recommended ? "min-h-[120px]" : "min-h-11"}`}
    >
      <div className="flex items-start justify-between gap-[var(--space-12)]">
        <div>
          <div className="flex flex-wrap items-center gap-[var(--space-8)]">
            <span className="font-[var(--font-title-2)] text-ink">
              {channel.name}
            </span>
            {recommended ? (
              <span className="rounded-fee-8 bg-accent-soft px-[var(--space-8)] py-[var(--space-4)] font-[var(--font-label)] text-accent">
                Recommended
              </span>
            ) : null}
          </div>
          <p className="mt-[var(--space-4)] font-[var(--font-body)] text-ink-secondary">
            {channel.meta}
          </p>
        </div>
        <span
          className={`mt-1 flex size-6 shrink-0 items-center justify-center rounded-full border-2 ${
            selected
              ? "border-ink bg-ink"
              : "border-border-strong bg-surface"
          }`}
          aria-hidden="true"
        >
          {selected ? (
            <span className="size-2 rounded-full bg-on-ink" />
          ) : null}
        </span>
      </div>
    </button>
  );
}

export function PayScreen({ studentId }: { studentId: string }) {
  const router = useRouter();
  const amountGroupId = useId();
  const [state, setState] = useState<LoadState<PayContext>>({
    status: "loading",
  });
  const [step, setStep] = useState<Step>(1);
  const [selection, setSelection] = useState<PaymentSelection>("instalment");
  const [channelId, setChannelId] = useState<string | null>(null);
  const [otherOpen, setOtherOpen] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [handoff, setHandoff] = useState<string | null>(null);
  const [submitError, setSubmitError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setState({ status: "loading" });
    const res = await fetchPayContext(studentId);
    setState(res);
  }, [studentId]);

  useEffect(() => {
    void load();
  }, [load]);

  const data = state.status === "success" ? state.data : null;

  const recommended = useMemo(
    () => (data ? pickRecommendedChannel(data.channels) : null),
    [data]
  );

  const others = useMemo(() => {
    if (!data || !recommended) return [];
    return sortChannelsByTier(data.channels).filter(
      (c) => c.id !== recommended.id
    );
  }, [data, recommended]);

  useEffect(() => {
    if (!recommended) return;
    // Never pre-select card when Tier 1 exists — recommended is highest tier.
    setChannelId((prev) => prev ?? recommended.id);
  }, [recommended]);

  const amount =
    selection === "instalment"
      ? data?.instalmentAmount ?? "0.00"
      : data?.fullBalanceAmount ?? "0.00";

  const selectedChannel =
    data?.channels.find((c) => c.id === channelId) ?? recommended;

  const onPay = async () => {
    if (!data || !selectedChannel) return;
    setSubmitError(null);
    setSubmitting(true);
    setHandoff(selectedChannel.handoffLabel);

    const result = await createPaymentAttempt({
      studentId: data.studentId,
      amount,
      selection,
      instalmentId:
        selection === "instalment" ? data.instalmentId ?? undefined : undefined,
      channelId: selectedChannel.id,
      idempotencyKey:
        typeof crypto !== "undefined" && "randomUUID" in crypto
          ? crypto.randomUUID()
          : `idem-${Date.now()}`,
    });

    if (!result.ok) {
      setSubmitting(false);
      setHandoff(null);
      setSubmitError(result.error.error.message);
      return;
    }

    const attempt = result.data;

    // Hand-off interstitial briefly, then system browser (never WebView).
    await new Promise((r) => setTimeout(r, 900));

    if (attempt.redirectUrl && typeof window !== "undefined") {
      window.open(attempt.redirectUrl, "_blank", "noopener,noreferrer");
    }

    setSubmitting(false);
    router.push(`/payments/attempts/${attempt.id}`);
  };

  if (state.status === "loading") return <ScreenLoading rows={5} />;
  if (state.status === "error") {
    return <ScreenError error={state.error} onRetry={() => void load()} />;
  }
  if (state.status === "empty" || !data) {
    return (
      <ScreenEmpty
        title="Nothing left to pay for this statement."
        actionLabel="Back to statement"
        onAction={() => router.push(`/students/${studentId}/statement`)}
      />
    );
  }

  if (handoff) {
    return (
      <ScreenBody>
        <div className="flex flex-1 flex-col items-center justify-center gap-[var(--space-16)] px-[var(--space-24)] text-center">
          <div
            className="fee-pending-ring"
            aria-hidden="true"
          />
          <p className="font-[var(--font-title-1)] text-ink">{handoff}</p>
          <p className="font-[var(--font-body)] text-ink-secondary">
            When you&apos;re done, you&apos;ll come right back here.
          </p>
        </div>
      </ScreenBody>
    );
  }

  const primaryLabel =
    step < 3 ? (
      "Continue"
    ) : (
      <>
        Pay <Amount>{formatPkr(amount)}</Amount>
      </>
    );

  return (
    <ScreenBody
      footer={
        <StickyActionBar>
          {submitError ? (
            <p
              role="alert"
              className="mb-[var(--space-8)] text-center font-[var(--font-body)] text-danger"
            >
              {submitError}
            </p>
          ) : null}
          {step === 3 ? (
            <div className="mb-[var(--space-12)] flex items-start gap-[var(--space-8)]">
              <Lock
                size={18}
                className="mt-0.5 shrink-0 text-ink-tertiary"
                aria-hidden="true"
              />
              <p className="font-[var(--font-label)] text-ink-secondary">
                <span className="inline-flex items-center gap-[var(--space-4)]">
                  <ShieldCheck size={16} aria-hidden="true" />
                  Paid directly to {data.institutionName}
                </span>
                <span className="block">A2Z never holds your money</span>
              </p>
            </div>
          ) : null}
          <Button
            type="button"
            className="w-full"
            loading={submitting}
            disabled={step === 2 && !channelId}
            onClick={() => {
              if (step === 1) setStep(2);
              else if (step === 2) setStep(3);
              else void onPay();
            }}
          >
            {primaryLabel}
          </Button>
          {step > 1 ? (
            <button
              type="button"
              className="mt-[var(--space-8)] inline-flex min-h-11 w-full items-center justify-center font-[var(--font-body-strong)] text-accent touch-manipulation"
              onClick={() => setStep((s) => (s === 3 ? 2 : 1))}
            >
              Back
            </button>
          ) : null}
        </StickyActionBar>
      }
    >
      <MockBanner />
      <div className="px-[var(--space-16)] pb-[var(--space-24)] pt-[var(--space-16)]">
        <Link
          href={`/students/${studentId}/statement`}
          className="inline-flex min-h-11 items-center font-[var(--font-body-strong)] text-accent touch-manipulation"
        >
          <span className="rtl:hidden">← </span>
          <span className="hidden rtl:inline">→ </span>
          Statement
        </Link>

        <p className="mt-[var(--space-12)] font-[var(--font-label)] text-ink-tertiary">
          Step {step} of 3
        </p>

        {step === 1 ? (
          <section className="mt-[var(--space-16)]" aria-labelledby={amountGroupId}>
            <h1 id={amountGroupId} className="font-[var(--font-title-1)] text-ink">
              What would you like to pay?
            </h1>
            <ul className="mt-[var(--space-16)] flex flex-col gap-[var(--space-12)]">
              <li>
                <button
                  type="button"
                  onClick={() => setSelection("instalment")}
                  aria-pressed={selection === "instalment"}
                  className={`w-full rounded-fee-16 border p-[var(--space-16)] text-start touch-manipulation ${
                    selection === "instalment"
                      ? "border-border-strong bg-surface shadow-fee-1"
                      : "border-border bg-surface"
                  }`}
                >
                  <p className="font-[var(--font-body-strong)] text-ink">
                    Next instalment
                  </p>
                  <p className="mt-[var(--space-8)] fee-title-1 tabular-nums text-ink">
                    <Amount>{formatPkr(data.instalmentAmount)}</Amount>
                  </p>
                  <p className="mt-[var(--space-4)] font-[var(--font-label)] text-ink-secondary">
                    Due <bdi>{formatDueDate(data.dueDate)}</bdi>
                  </p>
                </button>
              </li>
              <li>
                <button
                  type="button"
                  onClick={() => setSelection("full_balance")}
                  aria-pressed={selection === "full_balance"}
                  className={`w-full rounded-fee-16 border p-[var(--space-16)] text-start touch-manipulation ${
                    selection === "full_balance"
                      ? "border-border-strong bg-surface shadow-fee-1"
                      : "border-border bg-surface"
                  }`}
                >
                  <p className="font-[var(--font-body-strong)] text-ink">
                    Full balance
                  </p>
                  <p className="mt-[var(--space-8)] fee-title-1 tabular-nums text-ink">
                    <Amount>{formatPkr(data.fullBalanceAmount)}</Amount>
                  </p>
                  <p className="mt-[var(--space-4)] font-[var(--font-label)] text-ink-secondary">
                    Pay everything due for {data.termLabel}
                  </p>
                </button>
              </li>
            </ul>
          </section>
        ) : null}

        {step === 2 ? (
          <section className="mt-[var(--space-16)]">
            <h1 className="font-[var(--font-title-1)] text-ink">How to pay</h1>
            <p className="mt-[var(--space-8)] font-[var(--font-body)] text-ink-secondary">
              We recommend the fastest option with no extra charge.
            </p>
            {recommended ? (
              <div className="mt-[var(--space-16)]">
                <ChannelRow
                  channel={recommended}
                  selected={channelId === recommended.id}
                  onSelect={() => setChannelId(recommended.id)}
                  recommended
                />
              </div>
            ) : null}
            {others.length > 0 ? (
              <button
                type="button"
                onClick={() => setOtherOpen(true)}
                className="mt-[var(--space-12)] flex min-h-11 w-full items-center justify-between rounded-fee-12 border border-border bg-surface px-[var(--space-16)] py-[var(--space-12)] touch-manipulation"
              >
                <span className="font-[var(--font-body-strong)] text-ink">
                  Other ways to pay
                </span>
                <ChevronRight
                  size={20}
                  className="text-ink-tertiary rtl:rotate-180"
                  aria-hidden="true"
                />
              </button>
            ) : null}
            {selectedChannel && selectedChannel.id !== recommended?.id ? (
              <p className="mt-[var(--space-12)] font-[var(--font-label)] text-ink-secondary">
                Selected: {selectedChannel.name}
              </p>
            ) : null}
          </section>
        ) : null}

        {step === 3 ? (
          <section className="mt-[var(--space-16)]">
            <h1 className="font-[var(--font-title-1)] text-ink">Confirm payment</h1>
            <div
              className="fee-tenant-band mt-[var(--space-16)] rounded-fee-16 bg-surface p-[var(--space-16)] shadow-fee-1"
              style={{ ["--tenant-brand" as string]: data.tenantBrandCss }}
            >
              <p className="font-[var(--font-label)] text-ink-secondary">
                {data.institutionName}
              </p>
              <p className="mt-[var(--space-4)] font-[var(--font-title-2)] text-ink">
                {data.studentName}
              </p>
              <p className="mt-[var(--space-4)] font-[var(--font-label)] text-ink-secondary">
                {data.termLabel}
              </p>
              <p className="mt-[var(--space-16)] fee-display-amount tabular-nums text-ink">
                <Amount>{formatPkr(amount)}</Amount>
              </p>
              <p className="mt-[var(--space-8)] font-[var(--font-body)] text-ink-secondary">
                via {selectedChannel?.name ?? "—"}
              </p>
            </div>
            <p className="mt-[var(--space-16)] font-[var(--font-label)] text-ink-tertiary">
              No card number is entered here. Checkout opens with your bank or
              wallet.
            </p>
            <Link
              href={`/pay/${studentId}/share`}
              className="mt-[var(--space-16)] inline-flex min-h-11 items-center font-[var(--font-body-strong)] text-accent touch-manipulation"
            >
              Share a pay-on-behalf link
            </Link>
          </section>
        ) : null}
      </div>

      <ParentBottomSheet
        open={otherOpen}
        title="Other ways to pay"
        onClose={() => setOtherOpen(false)}
      >
        <ul className="flex flex-col gap-[var(--space-12)]">
          {others.map((channel) => (
            <li key={channel.id}>
              <ChannelRow
                channel={channel}
                selected={channelId === channel.id}
                onSelect={() => {
                  setChannelId(channel.id);
                  setOtherOpen(false);
                }}
              />
            </li>
          ))}
        </ul>
      </ParentBottomSheet>
    </ScreenBody>
  );
}
