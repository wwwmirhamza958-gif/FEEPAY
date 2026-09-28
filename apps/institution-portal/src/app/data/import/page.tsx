"use client";

import { Button, EmptyState, ErrorState } from "@fee/ui";
import { AlertTriangle, CheckCircle2, FileUp, SkipForward, XCircle } from "lucide-react";
import { useMemo, useState, type ReactNode } from "react";
import { ConfirmDialog } from "@/components/ConfirmDialog";
import { FieldLabel, MockBanner, PageHeader, Panel } from "@/components/ui-bits";
import {
  commitImport,
  fetchImportErrors,
  runImportDryRun,
  setImportMapping,
  uploadImport,
} from "@/lib/api/client";
import type { ImportDryRunCounts, LoadState } from "@/lib/types";

const STEPS = ["Upload", "Map", "Dry run", "Commit"] as const;
type Step = (typeof STEPS)[number];

const ENTITIES = [
  { value: "students", label: "Students" },
  { value: "enrolments", label: "Enrolments" },
  { value: "guardians", label: "Guardians" },
  { value: "programs", label: "Programs" },
] as const;

/** Canonical field options for students CSV (R-C-E-008). */
const STUDENT_FIELD_OPTIONS = [
  { value: "external_id", label: "external_id (required)" },
  { value: "display_name", label: "display_name (required)" },
  { value: "program_code", label: "program_code (required)" },
  { value: "intake_code", label: "intake_code" },
  { value: "skip", label: "— Skip column —" },
];

export default function ImportWizardPage() {
  const [stepIndex, setStepIndex] = useState(0);
  const [entity, setEntity] = useState<string>("students");
  const [file, setFile] = useState<File | null>(null);
  const [fileName, setFileName] = useState<string | null>(null);
  const [jobId, setJobId] = useState<string | null>(null);
  const [columns, setColumns] = useState<string[]>([]);
  const [mapping, setMapping] = useState<Record<string, string>>({});
  const [uploadError, setUploadError] = useState<string | null>(null);
  const [dryRun, setDryRun] = useState<LoadState<ImportDryRunCounts> | null>(null);
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [reason, setReason] = useState("");
  const [committing, setCommitting] = useState(false);
  const [commitDone, setCommitDone] = useState(false);
  const [uploading, setUploading] = useState(false);

  const step: Step = STEPS[stepIndex]!;
  const canNext = useMemo(() => {
    if (step === "Upload") return Boolean(file);
    if (step === "Map") {
      if (!jobId) return false;
      if (entity !== "students") return true;
      return Object.values(mapping).includes("external_id");
    }
    if (step === "Dry run") return dryRun?.status === "success";
    return false;
  }, [step, file, jobId, entity, mapping, dryRun]);

  async function parseCsvHeaders(f: File): Promise<string[]> {
    const text = await f.text();
    const first = text.split(/\r?\n/).find((l) => l.trim().length > 0) ?? "";
    return first.split(",").map((h) => h.trim()).filter(Boolean);
  }

  async function onUploadAndContinue() {
    if (!file) return;
    setUploading(true);
    setUploadError(null);
    const res = await uploadImport({ entity, file });
    setUploading(false);
    if (!res.ok) {
      setUploadError(res.error.error.message);
      return;
    }
    setJobId(res.jobId);
    const headers = await parseCsvHeaders(file);
    setColumns(headers);
    const initial: Record<string, string> = {};
    for (const h of headers) {
      const known = STUDENT_FIELD_OPTIONS.find((o) => o.value === h);
      initial[h] = known ? known.value : h;
    }
    setMapping(initial);
    setCommitDone(false);
    setDryRun(null);
    setStepIndex(1);
  }

  async function onMapContinue() {
    if (!jobId) return;
    // Mapping is field → column header for API; invert UI column → field.
    const apiMapping: Record<string, string> = {};
    for (const [col, field] of Object.entries(mapping)) {
      if (field && field !== "skip") apiMapping[field] = col;
    }
    const res = await setImportMapping(jobId, apiMapping);
    if (!res.ok) {
      setDryRun({ status: "error", error: res.error });
      setStepIndex(2);
      return;
    }
    setStepIndex(2);
    void runDryRun();
  }

  async function runDryRun() {
    if (!jobId) return;
    setDryRun({ status: "loading" });
    const res = await runImportDryRun(jobId);
    setDryRun(res);
  }

  async function onCommit() {
    if (!jobId) return;
    setCommitting(true);
    const res = await commitImport(jobId, reason.trim());
    setCommitting(false);
    if (!res.ok) {
      setDryRun({ status: "error", error: res.error });
      setConfirmOpen(false);
      return;
    }
    setConfirmOpen(false);
    setCommitDone(true);
  }

  async function onDownloadErrors() {
    if (!jobId) return;
    const res = await fetchImportErrors(jobId);
    if (!res.ok) {
      setDryRun({ status: "error", error: res.error });
      return;
    }
    const lines = [
      "rowNumber,field,code,message",
      ...res.rows.map(
        (r) => `${r.rowNumber},${r.field},${r.code},"${r.message.replace(/"/g, '""')}"`
      ),
    ];
    const blob = new Blob([lines.join("\n")], { type: "text/csv" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `import-errors-${jobId}.csv`;
    a.click();
    URL.revokeObjectURL(url);
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
        <Panel className="max-w-xl space-y-[var(--space-16)]">
          <div>
            <FieldLabel htmlFor="import-entity">Entity</FieldLabel>
            <select
              id="import-entity"
              value={entity}
              onChange={(e) => setEntity(e.target.value)}
              className="w-full rounded-fee-8 border border-border-strong bg-surface px-[var(--space-12)] py-[var(--space-8)] text-[15px] focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
            >
              {ENTITIES.map((e) => (
                <option key={e.value} value={e.value}>
                  {e.label}
                </option>
              ))}
            </select>
          </div>
          <label className="flex cursor-pointer flex-col items-center gap-[var(--space-12)] rounded-fee-12 border border-dashed border-border-strong bg-surface-sunken px-[var(--space-24)] py-[var(--space-32)]">
            <FileUp size={24} strokeWidth={1.75} aria-hidden="true" className="text-ink-secondary" />
            <span className="text-[15px] font-medium text-ink">
              {fileName ?? "Choose CSV file"}
            </span>
            <span className="text-[14px] text-ink-tertiary">
              Uploads to POST /api/v1/imports
            </span>
            <input
              type="file"
              accept=".csv,text/csv"
              className="sr-only"
              onChange={(e) => {
                const f = e.target.files?.[0] ?? null;
                setFile(f);
                setFileName(f?.name ?? null);
                setJobId(null);
                setCommitDone(false);
                setUploadError(null);
              }}
            />
          </label>
          {uploadError ? (
            <ErrorState message={uploadError} onRetry={() => setUploadError(null)} />
          ) : null}
        </Panel>
      ) : null}

      {step === "Map" ? (
        <Panel>
          <p className="mb-[var(--space-16)] text-[15px] text-ink-secondary">
            Map each CSV column to a canonical field. For students,{" "}
            <code>external_id</code> is required. Job: {jobId}
          </p>
          {columns.length === 0 ? (
            <EmptyState title="No columns detected in the CSV header." />
          ) : (
            <div className="grid gap-[var(--space-12)] sm:grid-cols-2">
              {columns.map((col) => (
                <label key={col} className="block">
                  <span className="mb-[var(--space-4)] block text-[14px] font-medium text-ink">
                    Column {col}
                  </span>
                  <select
                    value={mapping[col] ?? "skip"}
                    onChange={(e) =>
                      setMapping((m) => ({ ...m, [col]: e.target.value }))
                    }
                    className="w-full rounded-fee-8 border border-border-strong bg-surface px-[var(--space-12)] py-[var(--space-8)] text-[15px] focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
                  >
                    {STUDENT_FIELD_OPTIONS.map((o) => (
                      <option key={o.value} value={o.value}>
                        {o.label}
                      </option>
                    ))}
                  </select>
                </label>
              ))}
            </div>
          )}
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
                onClick={() => void onDownloadErrors()}
                className="font-semibold text-accent focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
              >
                Download error report
              </button>
            </p>
          ) : null}
        </div>
      ) : null}

      {step === "Commit" ? (
        <Panel className="max-w-xl">
          {commitDone ? (
            <p className="text-[15px] text-success" role="status">
              {/* TODO: use StatusPill Succeeded when import kinds are confirmed in @fee/ui */}
              Import commit accepted. Status: Succeeded (CONTRACT §2.4).
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
          disabled={stepIndex === 0 || uploading}
          onClick={() => setStepIndex((i) => Math.max(0, i - 1))}
        >
          Back
        </Button>
        {step === "Upload" ? (
          <Button
            size="portal"
            disabled={!file}
            loading={uploading}
            onClick={() => void onUploadAndContinue()}
          >
            Upload & continue
          </Button>
        ) : null}
        {step === "Map" ? (
          <Button size="portal" disabled={!canNext} onClick={() => void onMapContinue()}>
            Continue to Dry run
          </Button>
        ) : null}
        {step === "Dry run" ? (
          <Button
            size="portal"
            disabled={!canNext}
            onClick={() => setStepIndex(3)}
          >
            Continue to Commit
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
