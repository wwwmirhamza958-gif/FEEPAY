"use client";

import { Amount, Button, StatusPill } from "@fee/ui";
import { formatPkr } from "@fee/money";
import { CheckCircle2, XCircle } from "lucide-react";
import { useRouter } from "next/navigation";
import { useCallback, useEffect, useRef, useState } from "react";
import { StickyActionBar } from "@/components/PhoneShell";
import {
  ScreenBody,
  ScreenError,
  ScreenLoading,
} from "@/components/ScreenStates";
import {
  fetchParentPaymentAttempt,
  type ParentPaymentAttemptView,
} from "@/lib/parent/client";
import type { LoadState } from "@/lib/types";

const POLL_MS = 2_500;

function statusPillKind(
  status: ParentPaymentAttemptView["status"]
): "Paid" | "Pending" | "Failed" | "Issued" {
  switch (status) {
    case "Succeeded":
      return "Paid";
    case "Failed":
    case "Expired":
    case "Cancelled":
    case "Reversed":
      return "Failed";
    case "Pending":
    case "Created":
      return "Pending";
    default:
      return "Issued";
  }
}

function methodLabel(adapterCode: string): string {
  if (adapterCode.includes("jazzcash")) return "JazzCash";
  if (adapterCode.includes("easypaisa")) return "Easypaisa";
  if (adapterCode.includes("card")) return "Credit/Debit Card";
  return adapterCode;
}

export function PaymentStatusScreen({ attemptId }: { attemptId: string }) {
  const router = useRouter();
  const [state, setState] = useState<LoadState<ParentPaymentAttemptView>>({
    status: "loading",
  });
  const openedCheckout = useRef(false);

  const load = useCallback(async () => {
    const res = await fetchParentPaymentAttempt(attemptId);
    setState(res);
  }, [attemptId]);

  useEffect(() => {
    setState({ status: "loading" });
    void load();
  }, [load]);

  useEffect(() => {
    if (state.status !== "success") return;
    const { status, checkoutUrl } = state.data;
    if (
      (status === "Pending" || status === "Created") &&
      checkoutUrl &&
      !openedCheckout.current
    ) {
      openedCheckout.current = true;
      window.open(checkoutUrl, "_blank", "noopener,noreferrer");
    }
    if (status !== "Pending" && status !== "Created") return;

    const poll = window.setInterval(() => {
      void load();
    }, POLL_MS);
    return () => window.clearInterval(poll);
  }, [state, load]);

  if (state.status === "loading") return <ScreenLoading rows={4} />;
  if (state.status === "error") {
    return <ScreenError error={state.error} onRetry={() => void load()} />;
  }

  const attempt = state.data;
  const kind = statusPillKind(attempt.status);
  const terminalSuccess = attempt.status === "Succeeded";
  const terminalFail =
    attempt.status === "Failed" ||
    attempt.status === "Expired" ||
    attempt.status === "Cancelled" ||
    attempt.status === "Reversed";
  const pending = !terminalSuccess && !terminalFail;

  return (
    <>
      <ScreenBody className="pb-28" title="Payment status" hideChrome>
        <div className="flex flex-col items-center px-[var(--space-16)] pt-[var(--space-32)] pb-[var(--space-16)] text-center lg:px-[var(--space-24)]">
          {pending ? (
            <div
              className="fee-pending-ring"
              role="status"
              aria-label="Payment processing"
            />
          ) : terminalSuccess ? (
            <CheckCircle2 size={48} className="text-success" aria-hidden />
          ) : (
            <XCircle size={48} className="text-danger" aria-hidden />
          )}
          <h1 className="mt-[var(--space-20)] fee-title-1 text-ink">
            {terminalSuccess
              ? "Payment successful"
              : terminalFail
                ? "Payment failed"
                : "Payment processing"}
          </h1>
          <p className="mt-[var(--space-8)] max-w-[32ch] font-[var(--font-body)] text-ink-secondary">
            {terminalSuccess
              ? "Your fee payment has been recorded."
              : terminalFail
                ? "No money was taken, or the attempt did not complete. You can try again."
                : "Confirm the payment in the provider window. This screen updates automatically."}
          </p>
        </div>

        <section className="fee-amount-hero fee-enter-delay-1 mx-[var(--space-16)] overflow-hidden lg:mx-[var(--space-24)]">
          <div className="flex items-center justify-between gap-[var(--space-16)] border-b border-border px-[var(--space-16)] py-[var(--space-16)] ps-[calc(var(--space-16)+4px)]">
            <p className="font-[var(--font-label)] text-ink-secondary">Status</p>
            <StatusPill kind={kind} />
          </div>
          <div className="border-b border-border px-[var(--space-16)] py-[var(--space-16)] ps-[calc(var(--space-16)+4px)]">
            <p className="font-[var(--font-label)] text-ink-secondary">Amount</p>
            <p className="mt-[var(--space-4)] fee-display-amount tabular-nums text-ink">
              <Amount>{formatPkr(attempt.amount)}</Amount>
            </p>
          </div>
          <div className="px-[var(--space-16)] py-[var(--space-16)] ps-[calc(var(--space-16)+4px)]">
            <p className="font-[var(--font-label)] text-ink-secondary">Method</p>
            <p className="mt-[var(--space-4)] font-[var(--font-title-2)] text-ink">
              {methodLabel(attempt.adapterCode)}
            </p>
          </div>
        </section>
      </ScreenBody>

      <StickyActionBar flush>
        {terminalSuccess || terminalFail ? (
          <Button
            variant="primary"
            className="w-full"
            onClick={() => router.push("/home")}
          >
            Back to fee details
          </Button>
        ) : (
          <Button
            variant="secondary"
            className="w-full"
            onClick={() => void load()}
          >
            Refresh status
          </Button>
        )}
      </StickyActionBar>
    </>
  );
}
