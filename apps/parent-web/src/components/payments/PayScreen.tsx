"use client";

import { Amount, Button } from "@fee/ui";
import { formatPkr } from "@fee/money";
import { ShieldCheck } from "lucide-react";
import { useRouter } from "next/navigation";
import { useCallback, useEffect, useId, useState } from "react";
import { StickyActionBar } from "@/components/PhoneShell";
import {
  ScreenBody,
  ScreenError,
  ScreenLoading,
} from "@/components/ScreenStates";
import { formatDueDate } from "@/lib/format";
import {
  createParentPayment,
  fetchParentPayContext,
} from "@/lib/parent/client";
import type { ParentPayContext, ParentPaymentChannel } from "@/lib/parent/types";
import type { LoadState } from "@/lib/types";

function ChannelRow({
  channel,
  selected,
  recommended,
  onSelect,
}: {
  channel: ParentPaymentChannel;
  selected: boolean;
  recommended: boolean;
  onSelect: () => void;
}) {
  return (
    <button
      type="button"
      onClick={onSelect}
      aria-pressed={selected}
      className={`w-full rounded-fee-16 border p-[var(--space-16)] text-start touch-manipulation transition-[transform,box-shadow] duration-fee-press ease-fee-out active:scale-[0.98] min-h-11 ${
        selected
          ? "border-border-strong bg-surface shadow-fee-1"
          : "border-border bg-surface"
      }`}
    >
      <div className="flex items-start justify-between gap-[var(--space-12)]">
        <div className="min-w-0">
          <div className="flex flex-wrap items-center gap-[var(--space-8)]">
            <span className="font-[var(--font-title-2)] text-ink">
              {channel.displayName}
            </span>
            {recommended ? (
              <span className="rounded-fee-8 bg-accent-soft px-[var(--space-8)] py-[var(--space-2)] font-[var(--font-micro)] text-accent">
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
            selected ? "border-ink bg-ink" : "border-border-strong bg-surface"
          }`}
          aria-hidden="true"
        >
          {selected ? <span className="size-2 rounded-full bg-on-ink" /> : null}
        </span>
      </div>
    </button>
  );
}

export function PayScreen({ studentId }: { studentId: string }) {
  const router = useRouter();
  const channelGroupId = useId();
  const [state, setState] = useState<LoadState<ParentPayContext>>({
    status: "loading",
  });
  const [adapterCode, setAdapterCode] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [handoff, setHandoff] = useState<string | null>(null);
  const [submitError, setSubmitError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setState({ status: "loading" });
    const res = await fetchParentPayContext(studentId);
    setState(res);
    if (res.status === "success" && res.data.channels[0]) {
      setAdapterCode(res.data.channels[0].adapterCode);
    }
  }, [studentId]);

  useEffect(() => {
    void load();
  }, [load]);

  const data = state.status === "success" ? state.data : null;

  async function onConfirmPay() {
    if (!data || !adapterCode || submitting) return;
    setSubmitting(true);
    setSubmitError(null);
    const channel = data.channels.find((c) => c.adapterCode === adapterCode);
    setHandoff(channel?.handoffLabel ?? "Opening payment…");

    const res = await createParentPayment({
      studentId: data.fee.studentId,
      adapterCode,
      invoiceId: data.fee.invoiceId,
      idempotencyKey:
        typeof crypto !== "undefined" && "randomUUID" in crypto
          ? crypto.randomUUID()
          : `pay-${Date.now()}`,
    });

    if (!res.ok) {
      setSubmitting(false);
      setHandoff(null);
      setSubmitError(res.error.error.message);
      return;
    }

    if (res.checkoutUrl && typeof window !== "undefined") {
      // System browser only — never WebView / in-app card form.
      window.open(res.checkoutUrl, "_blank", "noopener,noreferrer");
    }

    router.push(`/payments/attempts/${res.attemptId}`);
  }

  if (state.status === "loading") return <ScreenLoading rows={5} />;
  if (state.status === "error") {
    return <ScreenError error={state.error} onRetry={() => void load()} />;
  }
  if (!data) return null;

  const fee = data.fee;
  const recommendedCode = data.channels[0]?.adapterCode;

  return (
    <>
      <ScreenBody
        className="pb-28"
        title="Payment"
        hideChrome
      >
        <div className="px-[var(--space-16)] pt-[var(--space-16)] lg:px-[var(--space-24)]">
          <p className="font-[var(--font-body)] text-ink-secondary">
            Review the amount, then choose how to pay.
          </p>

          <section className="fee-amount-hero fee-enter-delay-1 mt-[var(--space-16)] p-[var(--space-20)] ps-[calc(var(--space-20)+4px)]">
            <p className="font-[var(--font-label)] text-ink-secondary">
              Amount due
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
            <div className="mt-[var(--space-16)] border-t border-border pt-[var(--space-16)]">
              <p className="font-[var(--font-label)] text-ink-secondary">
                Student
              </p>
              <p className="mt-[var(--space-4)] font-[var(--font-title-2)] text-ink">
                {fee.studentName}
              </p>
              <p className="mt-[var(--space-4)] font-[var(--font-body)] text-ink-secondary">
                {fee.studentExternalId} · {fee.classLabel}
              </p>
            </div>
          </section>

          <section
            className="fee-enter-delay-2 mt-[var(--space-24)]"
            aria-labelledby={channelGroupId}
          >
            <h2 id={channelGroupId} className="fee-title-2 text-ink">
              Payment method
            </h2>
            <div className="mt-[var(--space-12)] space-y-[var(--space-12)]">
              {data.channels.map((channel) => (
                <ChannelRow
                  key={channel.adapterCode}
                  channel={channel}
                  selected={adapterCode === channel.adapterCode}
                  recommended={channel.adapterCode === recommendedCode}
                  onSelect={() => setAdapterCode(channel.adapterCode)}
                />
              ))}
            </div>
            <p className="mt-[var(--space-16)] flex items-start gap-[var(--space-8)] font-[var(--font-label)] text-ink-secondary">
              <ShieldCheck size={16} className="mt-0.5 shrink-0" aria-hidden />
              You confirm payment on the provider page. Card details are never
              entered in this app.
            </p>
            {submitError ? (
              <p
                className="mt-[var(--space-12)] rounded-fee-12 bg-danger-soft px-[var(--space-12)] py-[var(--space-12)] font-[var(--font-body)] text-danger"
                role="alert"
              >
                {submitError}
              </p>
            ) : null}
            {handoff ? (
              <p
                className="mt-[var(--space-12)] font-[var(--font-body)] text-ink-secondary"
                aria-live="polite"
              >
                {handoff}
              </p>
            ) : null}
          </section>
        </div>
      </ScreenBody>

      <StickyActionBar flush>
        <Button
          variant="primary"
          className="w-full"
          disabled={!adapterCode || submitting}
          onClick={() => void onConfirmPay()}
        >
          {submitting ? (
            "Processing…"
          ) : (
            <>
              Pay · <Amount>{formatPkr(fee.feeAmount)}</Amount>
            </>
          )}
        </Button>
      </StickyActionBar>
    </>
  );
}
