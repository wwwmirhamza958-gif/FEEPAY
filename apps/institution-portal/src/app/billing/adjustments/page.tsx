"use client";

import { Button } from "@fee/ui";
import { useState } from "react";
import { ConfirmDialog } from "@/components/ConfirmDialog";
import {
  FieldLabel,
  MockBanner,
  PageHeader,
  Panel,
  TextInput,
} from "@/components/ui-bits";

/**
 * Adjustments entry shell — no Billing EF tables invented here.
 * TODO: POST /api/v1/billing/adjustments once C publishes.
 */
export default function AdjustmentsPage() {
  const [studentNumber, setStudentNumber] = useState("");
  const [amount, setAmount] = useState("");
  const [note, setNote] = useState("");
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [reason, setReason] = useState("");
  const [saved, setSaved] = useState(false);

  const canSubmit =
    studentNumber.trim().length > 0 &&
    /^-?\d+\.\d{2}$/.test(amount.trim()) &&
    note.trim().length > 0;

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
            <FieldLabel htmlFor="adj-student">Student number</FieldLabel>
            <TextInput
              id="adj-student"
              value={studentNumber}
              onChange={(e) => setStudentNumber(e.target.value)}
            />
          </div>
          <div>
            <FieldLabel htmlFor="adj-amount">
              Amount (decimal string, e.g. −3500.00)
            </FieldLabel>
            <TextInput
              id="adj-amount"
              value={amount}
              onChange={(e) => setAmount(e.target.value)}
              inputMode="decimal"
              className="tabular-nums"
              placeholder="-3500.00"
            />
          </div>
          <div>
            <FieldLabel htmlFor="adj-note">Internal note</FieldLabel>
            <TextInput
              id="adj-note"
              value={note}
              onChange={(e) => setNote(e.target.value)}
            />
          </div>
        </div>
        <p className="mt-[var(--space-16)] text-[14px] text-ink-tertiary">
          TODO: POST /api/v1/billing/adjustments — supersedes invoice per CONTRACT §2.2
        </p>
        {saved ? (
          <p className="mt-[var(--space-12)] text-[14px] text-success" role="status">
            Adjustment recorded locally (mock). No ledger write until C API exists.
          </p>
        ) : null}
      </Panel>

      <ConfirmDialog
        open={confirmOpen}
        title="Post this adjustment?"
        description="This will supersede the current statement and create a credit/debit note. Parents must see what changed."
        reason={reason}
        onReasonChange={setReason}
        confirmLabel="Post adjustment"
        onCancel={() => setConfirmOpen(false)}
        onConfirm={() => {
          setConfirmOpen(false);
          setSaved(true);
        }}
      />
    </>
  );
}
