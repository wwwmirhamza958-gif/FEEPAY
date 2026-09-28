"use client";

import { Amount, Button, EmptyState, ErrorState } from "@fee/ui";
import { formatPkr } from "@fee/money";
import { AlertTriangle, CircleDashed } from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import { ConfirmDialog } from "@/components/ConfirmDialog";
import {
  FieldLabel,
  MockBanner,
  PageHeader,
  Panel,
  TableShell,
  TableSkeleton,
  Td,
  Th,
} from "@/components/ui-bits";
import {
  fetchTerms,
  issueStatements,
  runBillingPreview,
  type AcademicTerm,
} from "@/lib/api/client";
import type { LoadState, PreviewRun } from "@/lib/types";

export default function BillingPreviewPage() {
  const [terms, setTerms] = useState<LoadState<AcademicTerm[]>>({ status: "loading" });
  const [termId, setTermId] = useState("");
  const [state, setState] = useState<LoadState<PreviewRun> | null>(null);
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [reason, setReason] = useState("");
  const [issuing, setIssuing] = useState(false);
  const [issuedMsg, setIssuedMsg] = useState<string | null>(null);

  const loadTerms = useCallback(() => {
    setTerms({ status: "loading" });
    void fetchTerms().then((res) => {
      setTerms(res);
      if (res.status === "success" && res.data[0]) {
        setTermId((current) => current || res.data[0]!.id);
      }
    });
  }, []);

  useEffect(() => {
    loadTerms();
  }, [loadTerms]);

  async function onPreview() {
    if (!termId) return;
    setIssuedMsg(null);
    setState({ status: "loading" });
    const res = await runBillingPreview(termId);
    setState(res);
  }

  async function onIssue() {
    if (!state || state.status !== "success") return;
    setIssuing(true);
    const res = await issueStatements({
      previewId: state.data.id,
      termId: state.data.termId ?? termId,
      reason: reason.trim(),
    });
    setIssuing(false);
    if (!res.ok) {
      setState({ status: "error", error: res.error });
      setConfirmOpen(false);
      return;
    }
    setConfirmOpen(false);
    setIssuedMsg(`Issued ${res.issuedCount} statements. Status: Issued.`);
  }

  return (
    <>
      <PageHeader
        title="Preview & issue"
        description="Run a fee preview for a term, review outliers (icon + text), then issue immutable statements."
        primaryAction={
          <Button
            size="portal"
            variant={state?.status === "success" ? "secondary" : "primary"}
            onClick={() => void onPreview()}
            loading={state?.status === "loading"}
            disabled={!termId}
          >
            Run preview
          </Button>
        }
      />
      <MockBanner />

      <Panel className="mb-[var(--space-24)] max-w-md">
        <FieldLabel htmlFor="term">Term</FieldLabel>
        {terms.status === "loading" ? (
          <p className="text-[14px] text-ink-tertiary" aria-busy="true">
            Loading terms…
          </p>
        ) : null}
        {terms.status === "error" ? (
          <ErrorState message={terms.error.error.message} onRetry={loadTerms} />
        ) : null}
        {terms.status === "empty" ? (
          <EmptyState title="No terms available." />
        ) : null}
        {terms.status === "success" ? (
          <select
            id="term"
            value={termId}
            onChange={(e) => setTermId(e.target.value)}
            className="w-full rounded-fee-8 border border-border-strong bg-surface px-[var(--space-12)] py-[var(--space-8)] text-[15px] focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
          >
            {terms.data.map((t) => (
              <option key={t.id} value={t.id}>
                {t.name} ({t.code})
              </option>
            ))}
          </select>
        ) : null}
      </Panel>

      {!state ? (
        <EmptyState title="No preview yet. Run a preview for the selected term." />
      ) : null}

      {state?.status === "loading" ? <TableSkeleton rows={6} cols={5} /> : null}
      {state?.status === "empty" ? (
        <EmptyState title="Preview returned no students for this term." />
      ) : null}
      {state?.status === "error" ? (
        <ErrorState message={state.error.error.message} onRetry={() => void onPreview()} />
      ) : null}

      {state?.status === "success" ? (
        <>
          <div className="mb-[var(--space-16)] flex flex-wrap items-end justify-between gap-[var(--space-16)]">
            <div>
              <p className="text-[14px] text-ink-secondary">
                {state.data.termLabel} · {state.data.lineCount} drafts ·{" "}
                {state.data.outlierCount} outlier
                {state.data.outlierCount === 1 ? "" : "s"}
              </p>
              <p className="mt-[var(--space-4)] font-heading text-[length:24px] font-semibold text-ink">
                <Amount>{formatPkr(state.data.totalAmount)}</Amount>
                <span className="ms-[var(--space-8)] text-[14px] font-medium text-ink-secondary">
                  total
                </span>
              </p>
            </div>
            <Button
              size="portal"
              onClick={() => {
                setReason("");
                setConfirmOpen(true);
              }}
            >
              Issue statements
            </Button>
          </div>

          {issuedMsg ? (
            <p className="mb-[var(--space-16)] text-[14px] text-success" role="status">
              {issuedMsg}
            </p>
          ) : null}

          <TableShell caption="Preview draft statements">
            <thead>
              <tr>
                <Th>Student</Th>
                <Th>Program</Th>
                <Th align="end">Amount</Th>
                <Th>Status</Th>
                <Th>Flags</Th>
              </tr>
            </thead>
            <tbody>
              {state.data.lines.map((line) => (
                <tr key={line.studentNumber}>
                  <Td>
                    <div className="font-medium">{line.studentName}</div>
                    <div className="text-[14px] text-ink-tertiary">{line.studentNumber}</div>
                  </Td>
                  <Td>{line.programCode}</Td>
                  <Td align="end">
                    <Amount>{formatPkr(line.amount)}</Amount>
                  </Td>
                  <Td>
                    {/* TODO: use StatusPill Draft when @fee/ui exports it (Agent A). */}
                    <span
                      className="inline-flex items-center gap-[var(--space-4)] rounded-fee-8 bg-surface-sunken px-[var(--space-8)] py-[var(--space-4)] text-[14px] font-medium text-ink-secondary"
                      role="status"
                      aria-label="Draft"
                    >
                      <CircleDashed size={16} strokeWidth={1.75} aria-hidden="true" />
                      Draft
                    </span>
                  </Td>
                  <Td>
                    {line.isOutlier ? (
                      <span className="inline-flex items-start gap-[var(--space-8)] text-[14px] text-warning">
                        <AlertTriangle
                          size={16}
                          strokeWidth={1.75}
                          aria-hidden="true"
                          className="mt-[2px] shrink-0"
                        />
                        <span>
                          <span className="font-semibold">Outlier.</span>{" "}
                          {line.outlierReason ?? "Review before issuing."}
                        </span>
                      </span>
                    ) : (
                      <span className="text-[14px] text-ink-tertiary">None</span>
                    )}
                  </Td>
                </tr>
              ))}
            </tbody>
          </TableShell>
        </>
      ) : null}

      <ConfirmDialog
        open={confirmOpen}
        title="Issue these statements?"
        description="Issued statements are immutable. Parents will see them as payable. Provide a reason for the audit log."
        reason={reason}
        onReasonChange={setReason}
        confirmLabel="Issue statements"
        confirmVariant="primary"
        confirming={issuing}
        onCancel={() => setConfirmOpen(false)}
        onConfirm={() => void onIssue()}
      />
    </>
  );
}
