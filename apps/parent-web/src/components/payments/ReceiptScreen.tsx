"use client";

import { Amount, Button } from "@fee/ui";
import { formatPkr } from "@fee/money";
import { Download, Share2 } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { MockBanner, StickyActionBar } from "@/components/PhoneShell";
import {
  ScreenBody,
  ScreenError,
  ScreenLoading,
} from "@/components/ScreenStates";
import { formatPaidAt } from "@/lib/format";
import { fetchPaymentReceipt, type PaymentReceipt } from "@/lib/payments";
import type { LoadState } from "@/lib/types";

export function ReceiptScreen({ receiptId }: { receiptId: string }) {
  const router = useRouter();
  const [state, setState] = useState<LoadState<PaymentReceipt>>({
    status: "loading",
  });

  const load = useCallback(async () => {
    setState({ status: "loading" });
    setState(await fetchPaymentReceipt(receiptId));
  }, [receiptId]);

  useEffect(() => {
    void load();
  }, [load]);

  if (state.status === "loading") return <ScreenLoading rows={5} />;
  if (state.status === "error") {
    return <ScreenError error={state.error} onRetry={() => void load()} />;
  }
  if (state.status !== "success") {
    return (
      <ScreenError
        error={{
          error: {
            code: "RESOURCE_NOT_FOUND",
            message: "We couldn't find that receipt.",
            messageKey: "errors.resource_not_found",
            correlationId: "f0000102-aaaa-bbbb-cccc-dddddddddddd",
            details: [],
            retryable: true,
          },
        }}
        onRetry={() => void load()}
      />
    );
  }

  const data = state.data;

  const share = () => {
    const text = `Receipt ${data.receiptNumber}: paid ${formatPkr(data.amount)} to ${data.institutionName}.`;
    if (navigator.share) {
      void navigator.share({ text, url: data.qrPayload });
    } else {
      window.open(
        `https://wa.me/?text=${encodeURIComponent(text)}`,
        "_blank",
        "noopener,noreferrer"
      );
    }
  };

  return (
    <ScreenBody
      footer={
        <StickyActionBar>
          <Button
            type="button"
            className="w-full"
            onClick={() => {
              if (data.pdfUrl) {
                window.open(data.pdfUrl, "_blank", "noopener,noreferrer");
              } else {
                // PDF endpoint pending — still one primary action.
                window.print();
              }
            }}
          >
            <Download size={18} aria-hidden="true" />
            Download PDF
          </Button>
          <Button
            type="button"
            variant="secondary"
            className="mt-[var(--space-8)] w-full"
            onClick={share}
          >
            <Share2 size={18} aria-hidden="true" />
            Share
          </Button>
        </StickyActionBar>
      }
    >
      <MockBanner />
      <div className="px-[var(--space-16)] pb-[var(--space-24)] pt-[var(--space-16)]">
        <button
          type="button"
          onClick={() => router.back()}
          className="inline-flex min-h-11 items-center font-[var(--font-body-strong)] text-accent touch-manipulation"
        >
          Back
        </button>
        <Link
          href="/payments/history"
          className="ms-[var(--space-16)] inline-flex min-h-11 items-center font-[var(--font-label)] text-ink-secondary touch-manipulation"
        >
          History
        </Link>

        <article
          className="fee-tenant-band mt-[var(--space-16)] rounded-fee-16 border border-border bg-surface p-[var(--space-20)] shadow-fee-1"
          style={{ ["--tenant-brand" as string]: data.tenantBrandCss }}
        >
          <header className="border-b border-border pb-[var(--space-16)]">
            <p className="font-[var(--font-label)] text-ink-secondary">
              {data.institutionName}
            </p>
            <h1 className="mt-[var(--space-4)] font-[var(--font-title-1)] text-ink">
              Receipt
            </h1>
            <p className="mt-[var(--space-8)] font-[var(--font-body-strong)] tabular-nums text-ink">
              {data.receiptNumber}
            </p>
          </header>

          <p className="mt-[var(--space-16)] font-[var(--font-body)] text-ink">
            {data.studentName} · {data.termLabel}
          </p>
          <p className="mt-[var(--space-4)] font-[var(--font-label)] text-ink-secondary">
            <bdi>{formatPaidAt(data.paidAt)}</bdi> · {data.channelName}
          </p>

          <ul className="mt-[var(--space-16)] border-y border-border py-[var(--space-12)]">
            {data.lines.map((line) => (
              <li
                key={line.feeHeadName}
                className="flex items-center justify-between gap-[var(--space-12)] py-[var(--space-8)]"
              >
                <span className="font-[var(--font-body)] text-ink">
                  {line.feeHeadName}
                </span>
                <bdi className="font-[var(--font-body-strong)] tabular-nums text-ink">
                  <Amount>{formatPkr(line.amount)}</Amount>
                </bdi>
              </li>
            ))}
          </ul>

          <div className="mt-[var(--space-16)] flex items-center justify-between">
            <span className="font-[var(--font-body-strong)] text-ink">Paid</span>
            <span className="fee-title-1 tabular-nums text-ink">
              <Amount>{formatPkr(data.amount)}</Amount>
            </span>
          </div>

          <p className="mt-[var(--space-12)] font-[var(--font-micro)] text-ink-tertiary">
            Gateway ref <bdi>{data.gatewayReference}</bdi>
          </p>

          <div
            className="mx-auto mt-[var(--space-24)] flex size-32 items-center justify-center rounded-fee-8 border border-dashed border-border-strong bg-surface-sunken"
            aria-label="QR code placeholder"
          >
            <span className="px-[var(--space-8)] text-center font-[var(--font-micro)] text-ink-tertiary">
              QR verification placeholder
            </span>
          </div>
        </article>
      </div>
    </ScreenBody>
  );
}
