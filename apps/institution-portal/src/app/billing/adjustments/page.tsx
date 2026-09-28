"use client";

import { Button, EmptyState, ErrorState } from "@fee/ui";
import { useState } from "react";
import { ConfirmDialog } from "@/components/ConfirmDialog";
import {
  FieldLabel,
  MockBanner,
  PageHeader,
  Panel,
  TextInput,
} from "@/components/ui-bits";
import { submitAdjustment } from "@/lib/api/client";

/**
 * Adjustments — POST /api/v1/billing/adjustments (R-C-E-014).
 * Requires an Issued invoice id; confirm + reason for audit.
 */
export default function AdjustmentsPage() {
  const [invoiceId, setInvoiceId] = useState("");
  const [feeHeadCode, setFeeHeadCode] = useState("TUITION");
  const [amount, setAmount] = useState("");
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [reason, setReason] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [saved, setSaved] = useState(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  const canSubmit =
    /^[0-9a-fA-F-]{36}$/.test(invoiceId.trim()) &&
    feeHeadCode.trim().length > 0 &&
    /^-?\d+\.\d{2}$/.test(amount.trim());

  async function onConfirm() {
    setSubmitting(true);
    setErrorMsg(null);
    const res = await submitAdjustment({
      invoiceId: invoiceId.trim(),
      feeHeadCode: feeHeadCode.trim().toUpperCase(),
      amount: amount.trim(),
      reason: reason.trim(),
    });
    setSubmitting(false);
    if (!res.ok) {
      setErrorMsg(res.error.error.message);
      setConfirmOpen(false);
      return;
    }
    setConfirmOpen(false);
    setSaved(true);
  }

  return (
    <>
      <PageHeader
        title="Adjustments"
        description="Credit notes and balance adjustments. Destructive — confirm + reason required."
        primaryAction={
          <Button
            size="portal"
            disabled={!canSubmit}
            onClick={() => {
              setReason("");
              setSaved(false);
              setErrorMsg(null);
              setConfirmOpen(true);
            }}
          >
            Submit adjustment
          </Button>
        }
      />
      <MockBanner />

      <Panel className="max-w-xl">
        <div className="space-y-[var(--space-16)]">
          <div>
            <FieldLabel htmlFor="adj-invoice">Invoice id</FieldLabel>
            <TextInput
              id="adj-invoice"
              value={invoiceId}
              onChange={(e) => setInvoiceId(e.target.value)}
              placeholder="Guid of an Issued invoice"
              className="font-mono text-[14px]"
            />
          </div>
          <div>
            <FieldLabel htmlFor="adj-head">Fee head code</FieldLabel>
            <TextInput
              id="adj-head"
              value={feeHeadCode}
              onChange={(e) => setFeeHeadCode(e.target.value)}
              placeholder="TUITION"
            />
          </div>
          <div>
            <FieldLabel htmlFor="adj-amount">
              Amount (decimal string, e.g. 50000.00)
            </FieldLabel>
            <TextInput
              id="adj-amount"
              value={amount}
              onChange={(e) => setAmount(e.target.value)}
              inputMode="decimal"
              className="tabular-nums"
              placeholder="50000.00"
            />
          </div>
        </div>
        {errorMsg ? (
          <div className="mt-[var(--space-16)]">
            <ErrorState message={errorMsg} onRetry={() => setErrorMsg(null)} />
          </div>
        ) : null}
        {saved ? (
          <p className="mt-[var(--space-12)] text-[14px] text-success" role="status">
            Adjustment posted. Original invoice is Superseded.
          </p>
        ) : null}
        {!canSubmit && !errorMsg ? (
          <EmptyState title="Enter a valid invoice id, fee head, and amount (two decimal places)." />
        ) : null}
      </Panel>

      <ConfirmDialog
        open={confirmOpen}
        title="Post this adjustment?"
        description="This will supersede the current statement and create a credit/debit note. Parents must see what changed."
        reason={reason}
        onReasonChange={setReason}
        confirmLabel="Post adjustment"
        confirming={submitting}
        onCancel={() => setConfirmOpen(false)}
        onConfirm={() => void onConfirm()}
      />
    </>
  );
}
