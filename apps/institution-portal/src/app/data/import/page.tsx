"use client";

import { Button, EmptyState, ErrorState } from "@fee/ui";
import { AlertTriangle, CheckCircle2, FileUp, SkipForward, XCircle } from "lucide-react";
import { useMemo, useState, type ReactNode } from "react";
import { ConfirmDialog } from "@/components/ConfirmDialog";
import { MockBanner, PageHeader, Panel } from "@/components/ui-bits";
import { commitImport, runImportDryRun } from "@/lib/mock/client";
import type { ImportDryRunCounts, LoadState } from "@/lib/types";

const STEPS = ["Upload", "Map", "Dry run", "Commit"] as const;
type Step = (typeof STEPS)[number];

const FIELD_OPTIONS = [
  { value: "studentNumber", label: "Student number" },
  { value: "fullName", label: "Full name" },
  { value: "programCode", label: "Program code" },
  { value: "intakeLabel", label: "Intake" },
  { value: "skip", label: "— Skip column —" },
];

export default function ImportWizardPage() {
  const [stepIndex, setStepIndex] = useState(0);
  const [fileName, setFileName] = useState<string | null>(null);
  const [columns] = useState(["A", "B", "C", "D"]);
  const [mapping, setMapping] = useState<Record<string, string>>({
    A: "studentNumber",
    B: "fullName",
    C: "programCode",
    D: "intakeLabel",
  });
  const [dryRun, setDryRun] = useState<LoadState<ImportDryRunCounts> | null>(null);
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [reason, setReason] = useState("");
  const [committing, setCommitting] = useState(false);
  const [commitDone, setCommitDone] = useState(false);

  const step: Step = STEPS[stepIndex]!;
  const canNext = useMemo(() => {
    if (step === "Upload") return Boolean(fileName);
    if (step === "Map") return Object.values(mapping).includes("studentNumber");
    if (step === "Dry run") return dryRun?.status === "success";
    return false;
  }, [step, fileName, mapping, dryRun]);

  async function runDryRun() {
    setDryRun({ status: "loading" });
    const res = await runImportDryRun();
    setDryRun(res);
  }

  async function onCommit() {
    setCommitting(true);
    const res = await commitImport();
    setCommitting(false);
    if (!res.ok) {
      setDryRun({ status: "error", error: res.error });
      setConfirmOpen(false);
      return;
    }
    setConfirmOpen(false);
    setCommitDone(true);
  }

  return (
    <>
      <PageHeader
        title="Import wizard"
        description="Upload → Map → Dry run → Commit. Dry-run count tiles before any write."
      />
      <MockBanner />

      <nav aria-label="Import steps" className="mb-[var(--space-24)]">
        <ol className="flex flex-wrap gap-[var(--space-8)]">
          {STEPS.map((s, i) => {
            const active = i === stepIndex;
            const done = i < stepIndex;
            return (
              <li
                key={s}
                className={
                  active
                    ? "rounded-fee-8 bg-accent-soft px-[var(--space-12)] py-[var(--space-8)] text-[14px] font-semibold text-accent"
                    : done
                      ? "rounded-fee-8 bg-surface-sunken px-[var(--space-12)] py-[var(--space-8)] text-[14px] font-medium text-ink"
                      : "rounded-fee-8 border border-border px-[var(--space-12)] py-[var(--space-8)] text-[14px] text-ink-tertiary"
                }
                aria-current={active ? "step" : undefined}
              >
                {i + 1}. {s}
              </li>
            );
          })}
        </ol>
      </nav>

      {step === "Upload" ? (
        <Panel className="max-w-xl">
          <label className="flex cursor-pointer flex-col items-center gap-[var(--space-12)] rounded-fee-12 border border-dashed border-border-strong bg-surface-sunken px-[var(--space-24)] py-[var(--space-32)]">
            <FileUp size={24} strokeWidth={1.75} aria-hidden="true" className="text-ink-secondary" />
            <span className="text-[15px] font-medium text-ink">
              {fileName ?? "Choose CSV or Excel file"}
            </span>
            <span className="text-[14px] text-ink-tertiary">Mock upload — file stays in browser</span>
            <input
              type="file"
              accept=".csv,.xlsx,.xls"
              className="sr-only"
              onChange={(e) => {
                const f = e.target.files?.[0];
                setFileName(f?.name ?? null);
                setCommitDone(false);
              }}
            />
          </label>
        </Panel>
      ) : null}

      {step === "Map" ? (
        <Panel>
          <p className="mb-[var(--space-16)] text-[15px] text-ink-secondary">
            Map each column to a canonical field. Student number is required.
          </p>
          <div className="grid gap-[var(--space-12)] sm:grid-cols-2">
            {columns.map((col) => (
              <label key={col} className="block">
                <span className="mb-[var(--space-4)] block text-[14px] font-medium text-ink">
                  Column {col}
                </span>
                <select
                  value={mapping[col]}
                  onChange={(e) =>
                    setMapping((m) => ({ ...m, [col]: e.target.value }))
                  }
                  className="w-full rounded-fee-8 border border-border-strong bg-surface px-[var(--space-12)] py-[var(--space-8)] text-[15px] focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
                >
                  {FIELD_OPTIONS.map((o) => (
                    <option key={o.value} value={o.value}>
                      {o.label}
                    </option>
                  ))}
                </select>
              </label>
            ))}
          </div>
        </Panel>
      ) : null}

      {step === "Dry run" ? (
        <div className="space-y-[var(--space-16)]">
          {!dryRun || dryRun.status === "loading" ? (
            <Panel>
              <p className="text-[15px] text-ink-secondary" aria-busy="true">
                {dryRun?.status === "loading"
                  ? "Validating rows…"
                  : "Run a dry run to see create / update / skip / error counts."}
              </p>
              {dryRun?.status !== "loading" ? (
                <Button
                  size="portal"
                  className="mt-[var(--space-16)]"
                  onClick={() => void runDryRun()}
                >
                  Run dry run
                </Button>
              ) : null}
            </Panel>
          ) : null}
          {dryRun?.status === "error" ? (
            <ErrorState
              message={dryRun.error.error.message}
              onRetry={() => void runDryRun()}
            />
          ) : null}
          {dryRun?.status === "empty" ? (
            <EmptyState title="Dry run returned no rows." />
          ) : null}
          {dryRun?.status === "success" ? (
            <div className="grid gap-[var(--space-16)] sm:grid-cols-2 lg:grid-cols-4">
              <CountTile
                label="Create"
                count={dryRun.data.create}
                icon={<CheckCircle2 size={20} strokeWidth={1.75} aria-hidden="true" />}
              />
              <CountTile
                label="Update"
                count={dryRun.data.update}
                icon={<AlertTriangle size={20} strokeWidth={1.75} aria-hidden="true" />}
              />
              <CountTile
                label="Skip"
                count={dryRun.data.skip}
                icon={<SkipForward size={20} strokeWidth={1.75} aria-hidden="true" />}
              />
              <CountTile
                label="Error"
                count={dryRun.data.error}
                icon={<XCircle size={20} strokeWidth={1.75} aria-hidden="true" />}
                danger
              />
            </div>
          ) : null}
          {dryRun?.status === "success" && dryRun.data.error > 0 ? (
            <p className="text-[14px] text-ink-secondary">
              <button
                type="button"
                className="font-semibold text-accent focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
              >
                Download error report
              </button>{" "}
              (mock — one click away per §9)
            </p>
          ) : null}
        </div>
      ) : null}

      {step === "Commit" ? (
        <Panel className="max-w-xl">
          {commitDone ? (
            <p className="text-[15px] text-success" role="status">
              Import commit accepted (mock). Status vocabulary: Succeeded (CONTRACT §2.4).
            </p>
          ) : (
            <>
              <p className="text-[15px] text-ink-secondary">
                Committing writes enrolment and student rows. This is audit-logged and needs a
                reason.
              </p>
              <Button
                size="portal"
                className="mt-[var(--space-16)]"
                onClick={() => {
                  setReason("");
                  setConfirmOpen(true);
                }}
              >
                Commit import
              </Button>
            </>
          )}
        </Panel>
      ) : null}

      <div className="mt-[var(--space-24)] flex flex-wrap gap-[var(--space-8)]">
        <Button
          size="portal"
          variant="secondary"
          disabled={stepIndex === 0}
          onClick={() => setStepIndex((i) => Math.max(0, i - 1))}
        >
          Back
        </Button>
        {stepIndex < STEPS.length - 1 ? (
          <Button
            size="portal"
            disabled={!canNext}
            onClick={() => {
              const next = stepIndex + 1;
              setStepIndex(next);
              if (STEPS[next] === "Dry run" && !dryRun) {
                void runDryRun();
              }
            }}
          >
            Continue to {STEPS[stepIndex + 1]}
          </Button>
        ) : null}
      </div>

      <ConfirmDialog
        open={confirmOpen}
        title="Commit this import?"
        description="Rows marked create/update will be written. This cannot be silently undone."
        reason={reason}
        onReasonChange={setReason}
        confirmLabel="Commit import"
        confirming={committing}
        onCancel={() => setConfirmOpen(false)}
        onConfirm={() => void onCommit()}
      />
    </>
  );
}

function CountTile({
  label,
  count,
  icon,
  danger,
}: {
  label: string;
  count: number;
  icon: ReactNode;
  danger?: boolean;
}) {
  return (
    <div
      className={
        danger
          ? "rounded-fee-16 border border-danger bg-danger-soft p-[var(--space-20)]"
          : "rounded-fee-16 border border-border bg-surface p-[var(--space-20)] shadow-fee-1"
      }
    >
      <div className="flex items-center gap-[var(--space-8)] text-ink-secondary">
        {icon}
        <span className="text-[14px] font-medium">{label}</span>
      </div>
      <p className="mt-[var(--space-8)] font-heading text-[length:32px] leading-10 font-semibold tabular-nums text-ink">
        {count}
      </p>
    </div>
  );
}
