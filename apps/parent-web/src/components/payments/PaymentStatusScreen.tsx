"use client";

import { Amount, Button, StatusPill } from "@fee/ui";
import { formatPkr } from "@fee/money";
import { Check, Copy, Download, Share2 } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useCallback, useEffect, useRef, useState } from "react";
import { MockBanner, StickyActionBar } from "@/components/PhoneShell";
import {
  ScreenBody,
  ScreenError,
  ScreenLoading,
} from "@/components/ScreenStates";
import { formatPaidAt } from "@/lib/format";
import {
  fetchPaymentAttempt,
  type PaymentAttempt,
} from "@/lib/payments";
import type { LoadState } from "@/lib/types";

const PENDING_HINT_MS = 20_000;
const POLL_MS = 2_500;

export function PaymentStatusScreen({ attemptId }: { attemptId: string }) {
  const router = useRouter();
  const [state, setState] = useState<LoadState<PaymentAttempt>>({
    status: "loading",
  });
  const [longPending, setLongPending] = useState(false);
  const [copied, setCopied] = useState(false);
  const mountedAt = useRef<number>(Date.now());

  const load = useCallback(async () => {
    const res = await fetchPaymentAttempt(attemptId);
    setState(res);
  }, [attemptId]);

  useEffect(() => {
    setState({ status: "loading" });
    mountedAt.current = Date.now();
    setLongPending(false);
    void load();
  }, [load]);

  useEffect(() => {
    if (state.status !== "success") return;
    const status = state.data.status;
    if (status !== "Pending" && status !== "Created") return;

    const poll = window.setInterval(() => {
      void load();
      if (Date.now() - mountedAt.current >= PENDING_HINT_MS) {
        setLongPending(true);
      }
    }, POLL_MS);

    const hint = window.setTimeout(() => setLongPending(true), PENDING_HINT_MS);

    return () => {
      window.clearInterval(poll);
      window.clearTimeout(hint);
    };
  }, [state, load]);

  const copyVoucher = async (value: string) => {
    try {
      await navigator.clipboard.writeText(value);
      setCopied(true);
      window.setTimeout(() => setCopied(false), 1500);
    } catch {
      /* ignore */
    }
  };

  if (state.status === "loading") return <ScreenLoading rows={4} />;
  if (state.status === "error") {
    return (
      <ScreenError
        error={state.error}
        onRetry={() => {
          setState({ status: "loading" });
          void load();
        }}
      />
    );
  }
  if (state.status === "empty") {
    return (
      <ScreenError
        error={{
          error: {
            code: "RESOURCE_NOT_FOUND",
            message: "We couldn't find that payment.",
            messageKey: "errors.resource_not_found",
            correlationId: "f0000101-aaaa-bbbb-cccc-dddddddddddd",
            details: [],
            retryable: true,
          },
        }}
        onRetry={() => void load()}
      />
    );
  }

  const data = state.data;
  const isPending = data.status === "Pending" || data.status === "Created";
  const isSuccess = data.status === "Succeeded";
  const isFailed = data.status === "Failed" || data.status === "Expired";
  const isVoucher = Boolean(data.voucherConsumerNumber);

  let footer = null;
  if (isSuccess && data.receiptId) {
    footer = (
      <StickyActionBar>
        <Button
          type="button"
          className="w-full"
          onClick={() => router.push(`/payments/receipts/${data.receiptId}`)}
        >
          <Download size={18} aria-hidden="true" />
          Download receipt
        </Button>
        <Button
          type="button"
          variant="secondary"
          className="mt-[var(--space-8)] w-full"
          onClick={() => {
            const text = `Paid ${formatPkr(data.amount)} to ${data.institutionName}. Receipt ready in Fee Platform.`;
            if (navigator.share) {
              void navigator.share({ text });
            } else {
              window.open(
                `https://wa.me/?text=${encodeURIComponent(text)}`,
                "_blank",
                "noopener,noreferrer"
              );
            }
          }}
        >
          <Share2 size={18} aria-hidden="true" />
          Share on WhatsApp
        </Button>
      </StickyActionBar>
    );
  } else if (isFailed) {
    footer = (
      <StickyActionBar>
        <Button
          type="button"
          className="w-full"
          onClick={() => router.push(`/pay/${data.studentId}`)}
        >
          Try again
        </Button>
        <Button
          type="button"
          variant="secondary"
          className="mt-[var(--space-8)] w-full"
          onClick={() => router.push(`/pay/${data.studentId}`)}
        >
          Try another way
        </Button>
      </StickyActionBar>
    );
  } else if (isVoucher && isPending) {
    footer = (
      <StickyActionBar>
        <Button
          type="button"
          className="w-full"
          onClick={() => void copyVoucher(data.voucherConsumerNumber!)}
        >
          {copied ? (
            <>
              <Check size={18} aria-hidden="true" />
              Copied
            </>
          ) : (
            <>
              <Copy size={18} aria-hidden="true" />
              Copy consumer number
            </>
          )}
        </Button>
      </StickyActionBar>
    );
  }
  // Pending (non-voucher): NO retry that could create a second payment.

  return (
    <ScreenBody footer={footer}>
      <MockBanner />
      <div className="px-[var(--space-16)] pb-[var(--space-24)] pt-[var(--space-16)]">
        <Link
          href="/home"
          className="inline-flex min-h-11 items-center font-[var(--font-body-strong)] text-accent touch-manipulation"
        >
          Home
        </Link>

        <div className="mt-[var(--space-20)] flex flex-col items-center text-center">
          {isPending && !isVoucher ? (
            <>
              <div className="fee-pending-ring" role="status" aria-label="Confirming" />
              <h1 className="mt-[var(--space-20)] font-[var(--font-title-1)] text-ink">
                Confirming with your bank…
              </h1>
              <p className="mt-[var(--space-8)] font-[var(--font-body)] text-ink-secondary">
                {longPending
                  ? "Still confirming. You can safely close this page. We'll send the receipt on WhatsApp."
                  : "This usually takes a few seconds."}
              </p>
            </>
          ) : null}

          {isSuccess ? (
            <>
              <StatusPill kind="Succeeded" />
              <h1 className="mt-[var(--space-16)] font-[var(--font-title-1)] text-ink">
                Payment received
              </h1>
              <p className="mt-[var(--space-12)] fee-display-amount tabular-nums text-ink">
                <Amount>{formatPkr(data.amount)}</Amount>
              </p>
              <p className="mt-[var(--space-8)] font-[var(--font-body)] text-ink-secondary">
                {data.channelName} · <bdi>{formatPaidAt(data.createdAt)}</bdi>
              </p>
              <p className="mt-[var(--space-16)] font-[var(--font-label)] text-ink-tertiary">
                Receipt opens from the button below.
              </p>
            </>
          ) : null}

          {isFailed ? (
            <>
              <StatusPill kind={data.status === "Expired" ? "Expired" : "Failed"} />
              <h1 className="mt-[var(--space-16)] font-[var(--font-title-1)] text-ink">
                {data.status === "Expired" ? "Payment expired" : "Payment failed"}
              </h1>
              <p className="mt-[var(--space-12)] font-[var(--font-body)] text-ink-secondary">
                {data.failureReason ??
                  "We couldn't complete this payment."}
              </p>
              {!data.moneyTaken ? (
                <p className="mt-[var(--space-8)] font-[var(--font-body-strong)] text-success">
                  No money was taken.
                </p>
              ) : null}
            </>
          ) : null}

          {isVoucher && isPending ? (
            <>
              <StatusPill kind="Pending" />
              <h1 className="mt-[var(--space-16)] font-[var(--font-title-1)] text-ink">
                Pay with consumer number
              </h1>
              <p className="mt-[var(--space-12)] fee-display-amount tabular-nums text-ink">
                <Amount>{formatPkr(data.amount)}</Amount>
              </p>
              <p
                className="mt-[var(--space-20)] font-[var(--font-title-1)] tracking-widest tabular-nums text-ink"
                aria-label="Consumer number"
              >
                <bdi>{data.voucherConsumerNumber}</bdi>
              </p>
              <ol className="mt-[var(--space-20)] w-full list-decimal space-y-[var(--space-8)] ps-[var(--space-20)] text-start font-[var(--font-body)] text-ink-secondary">
                <li>Open your bank app, ATM, or visit a branch.</li>
                <li>Choose bill payment / 1Link biller.</li>
                <li>Enter the consumer number above.</li>
                <li>Confirm the amount matches.</li>
              </ol>
              <p className="mt-[var(--space-16)] font-[var(--font-label)] text-ink-tertiary">
                This page updates on its own when payment lands.
              </p>
            </>
          ) : null}
        </div>
      </div>
    </ScreenBody>
  );
}
