/**
 * Live P1 API client — REQUESTS_TO_C.md R-C-E-001…014.
 * Paths match FeePlatform.Api BillingEndpoints / ImportEndpoints.
 */

import type { ApiErrorEnvelope } from "@fee/api-client";
import type {
  BrandingConfig,
  FeeHead,
  FeeScheduleVersion,
  ImportDryRunCounts,
  InstalmentPlan,
  LoadState,
  PreviewRun,
  Program,
  StudentRow,
} from "../types";
import { getTenantId } from "./config";
import { ApiRequestError, apiFetch, toLoadError } from "./http";

type Ok<T = void> = { ok: true } & (T extends void ? object : T);
type Err = { ok: false; error: ApiErrorEnvelope };
type Result<T = void> = Ok<T> | Err;

async function asListState<T>(fn: () => Promise<T[]>): Promise<LoadState<T[]>> {
  try {
    const data = await fn();
    if (data.length === 0) return { status: "empty" };
    return { status: "success", data };
  } catch (err) {
    return { status: "error", error: toLoadError(err) };
  }
}

async function asItemState<T>(fn: () => Promise<T | null>): Promise<LoadState<T>> {
  try {
    const data = await fn();
    if (data == null) return { status: "empty" };
    return { status: "success", data };
  } catch (err) {
    return { status: "error", error: toLoadError(err) };
  }
}

function failResult(err: unknown): Err {
  return { ok: false, error: toLoadError(err) };
}

/* ——— Wire DTOs (camelCase from ASP.NET) ——— */

type BrandingDto = {
  tenantId: string;
  displayName: string;
  brandColourHex: string;
  logoObjectKey: string | null;
};

type ProgramDto = {
  id: string;
  code: string;
  name: string;
  externalId: string | null;
};

type FeeHeadDto = {
  code: string;
  name: string;
  taxableFor236I: boolean;
};

type FeeScheduleDto = {
  id?: string;
  name?: string;
  version?: number;
  effectiveFrom?: string;
  status?: string;
};

type InstalmentPlanDto = {
  id: string;
  code: string;
  name: string;
  programId: string | null;
  parts: { sequence: number; share: number; dueOffsetDays: number; label: string }[];
};

type StudentDto = {
  id: string;
  externalId: string;
  displayName: string;
  programId: string | null;
  intakeId: string | null;
};

type TermDto = {
  id: string;
  code: string;
  name: string;
  startDate: string;
  endDate: string;
  externalId: string | null;
};

type PreviewDto = {
  previewRunId: string;
  termId: string;
  studentCount: number;
  totalAmount: string;
  outlierCount: number;
  results: {
    studentId: string;
    studentExternalId: string;
    totalAmount: string;
    isOutlier: boolean;
    outlierReason: string | null;
    lines: { feeHeadCode: string; amount: string; trace: string }[];
  }[];
};

type IssueDto = {
  termId: string;
  issuedCount: number;
};

type ImportJobDto = {
  jobId: string;
  entity: string;
  status: string;
  originalFileName: string;
  createdCount: number;
  updatedCount: number;
  skippedCount: number;
  errorCount: number;
};

type ImportDryRunDto = {
  jobId: string;
  status: string;
  wouldCreate: number;
  wouldUpdate: number;
  wouldSkip: number;
  errorCount: number;
  errors: { rowNumber: number; field: string; code: string; message: string }[];
};

type DiscountCategoryDto = {
  id?: string;
  name?: string;
  code?: string;
  stackingNote?: string;
};

export type AcademicTerm = {
  id: string;
  code: string;
  name: string;
};

export type DiscountCategory = {
  id: string;
  name: string;
  note: string;
};

export type AdjustmentInput = {
  invoiceId: string;
  reason: string;
  feeHeadCode: string;
  amount: string;
};

/* ——— R-C-E-001 Branding ——— */

export async function fetchBranding(): Promise<LoadState<BrandingConfig>> {
  return asItemState(async () => {
    const dto = await apiFetch<BrandingDto>(`/api/v1/tenants/${getTenantId()}/branding`);
    return {
      tenantName: dto.displayName,
      brandColourTokenNote:
        "Tenant colour is identity only (logo chip / 3 px band). Action colours stay product tokens.",
      brandColourCssVar: "var(--tenant-brand)",
      brandColourHex: dto.brandColourHex,
    } satisfies BrandingConfig;
  });
}

export async function saveBranding(
  patch: Pick<BrandingConfig, "tenantName"> & { brandColourHex?: string }
): Promise<Result> {
  try {
    await apiFetch(`/api/v1/tenants/${getTenantId()}/branding`, {
      method: "PATCH",
      body: {
        displayName: patch.tenantName,
        brandColourHex: patch.brandColourHex,
      },
    });
    return { ok: true };
  } catch (err) {
    return failResult(err);
  }
}

/* ——— R-C-E-002 Programs ——— */

export async function fetchPrograms(): Promise<LoadState<Program[]>> {
  return asListState(async () => {
    const items = await apiFetch<ProgramDto[]>("/api/v1/academic/programs");
    return items.map((p) => ({
      id: p.id,
      code: p.code,
      name: p.name,
      intakeLabel: p.externalId ?? "—",
    }));
  });
}

export async function createProgram(input: {
  code: string;
  name: string;
  intakeLabel: string;
}): Promise<Result<{ id: string }>> {
  try {
    const created = await apiFetch<ProgramDto>("/api/v1/academic/programs", {
      method: "POST",
      body: {
        code: input.code,
        name: input.name,
        externalId: input.intakeLabel || null,
      },
    });
    return { ok: true, id: created.id };
  } catch (err) {
    return failResult(err);
  }
}

/* ——— R-C-E-003 Fee heads ——— */

export async function fetchFeeHeads(): Promise<LoadState<FeeHead[]>> {
  return asListState(async () => {
    const items = await apiFetch<FeeHeadDto[]>("/api/v1/billing/fee-heads");
    return items.map((h) => ({
      id: h.code,
      code: h.code,
      name: h.name,
      taxableFor236I: h.taxableFor236I,
    }));
  });
}

export async function createFeeHead(input: {
  code: string;
  name: string;
}): Promise<Result<{ id: string }>> {
  try {
    await apiFetch("/api/v1/billing/fee-heads", {
      method: "POST",
      body: input,
    });
    return { ok: true, id: input.code };
  } catch (err) {
    return failResult(err);
  }
}

/* ——— R-C-E-004 Fee schedules (empty stub until B) ——— */

export async function fetchFeeSchedules(): Promise<LoadState<FeeScheduleVersion[]>> {
  return asListState(async () => {
    const items = await apiFetch<FeeScheduleDto[]>("/api/v1/billing/fee-schedules");
    return items.map((s, i) => ({
      id: s.id ?? `schedule-${i}`,
      name: s.name ?? "Untitled",
      version: s.version ?? 0,
      effectiveFrom: s.effectiveFrom ?? "—",
      status: (s.status === "Published" ? "Published" : "Draft") as FeeScheduleVersion["status"],
    }));
  });
}

/* ——— R-C-E-005 Discounts ——— */

export async function fetchDiscountCategories(): Promise<LoadState<DiscountCategory[]>> {
  return asListState(async () => {
    const items = await apiFetch<DiscountCategoryDto[]>("/api/v1/billing/discount-categories");
    return items.map((d, i) => ({
      id: d.id ?? d.code ?? `discount-${i}`,
      name: d.name ?? d.code ?? "Untitled",
      note: d.stackingNote ?? "",
    }));
  });
}

export async function createDiscountCategory(input: {
  name: string;
}): Promise<Result<{ id: string }>> {
  try {
    const created = await apiFetch<DiscountCategoryDto>("/api/v1/billing/discount-categories", {
      method: "POST",
      body: input,
    });
    return { ok: true, id: created.id ?? created.code ?? input.name };
  } catch (err) {
    return failResult(err);
  }
}

/* ——— R-C-E-006 Instalment plans ——— */

export async function fetchInstalmentPlans(): Promise<LoadState<InstalmentPlan[]>> {
  return asListState(async () => {
    const items = await apiFetch<InstalmentPlanDto[]>("/api/v1/billing/instalment-plans");
    return items.map((p) => ({
      id: p.id,
      name: p.name || p.code,
      parts: p.parts?.length ?? 0,
      note: (p.parts ?? [])
        .map((part) => `${part.label || `Part ${part.sequence}`}: ${Math.round(Number(part.share) * 100)}%`)
        .join(" · ") || p.code,
    }));
  });
}

/* ——— R-C-E-011 Students ——— */

export async function searchStudents(query: string): Promise<LoadState<StudentRow[]>> {
  try {
    const [students, programs] = await Promise.all([
      apiFetch<StudentDto[]>("/api/v1/academic/students", { query: { q: query.trim() || undefined } }),
      apiFetch<ProgramDto[]>("/api/v1/academic/programs").catch(() => [] as ProgramDto[]),
    ]);
    if (students.length === 0) return { status: "empty" };
    const programById = new Map(programs.map((p) => [p.id, p.code]));
    return {
      status: "success",
      data: students.map((s) => ({
        id: s.id,
        studentNumber: s.externalId,
        fullName: s.displayName,
        programCode: (s.programId && programById.get(s.programId)) || "—",
        intakeLabel: s.intakeId ?? "—",
      })),
    };
  } catch (err) {
    return { status: "error", error: toLoadError(err) };
  }
}

export type AddStudentInput = {
  externalId: string;
  displayName: string;
  programId: string;
  termId: string;
  fatherMobile: string;
  fatherDisplayName?: string;
};

export type AddStudentResult = {
  studentId: string;
  externalId: string;
  displayName: string;
  guardianId: string;
  fatherMobileE164: string;
  invoiceId: string;
  feeAmount: string;
  dueDate: string;
  paymentStatus: string;
};

export async function createStudent(
  input: AddStudentInput
): Promise<{ ok: true; data: AddStudentResult } | { ok: false; error: ApiErrorEnvelope }> {
  try {
    const data = await apiFetch<AddStudentResult>("/api/v1/academic/students", {
      method: "POST",
      body: {
        externalId: input.externalId,
        displayName: input.displayName,
        programId: input.programId,
        termId: input.termId,
        fatherMobile: input.fatherMobile,
        fatherDisplayName: input.fatherDisplayName || undefined,
        relation: "Father",
      },
    });
    return { ok: true, data };
  } catch (err) {
    return failResult(err) as { ok: false; error: ApiErrorEnvelope };
  }
}

/* ——— Terms (for preview term picker) ——— */

export async function fetchTerms(): Promise<LoadState<AcademicTerm[]>> {
  return asListState(async () => {
    const items = await apiFetch<TermDto[]>("/api/v1/academic/terms");
    return items.map((t) => ({ id: t.id, code: t.code, name: t.name }));
  });
}

/* ——— R-C-E-007…010 Import ——— */

export async function uploadImport(input: {
  entity: string;
  file: File;
}): Promise<Result<{ jobId: string }>> {
  try {
    const form = new FormData();
    form.append("entity", input.entity);
    form.append("file", input.file, input.file.name);
    const job = await apiFetch<ImportJobDto>("/api/v1/imports", { formData: form });
    return { ok: true, jobId: job.jobId };
  } catch (err) {
    return failResult(err);
  }
}

export async function setImportMapping(
  jobId: string,
  mapping: Record<string, string>
): Promise<Result> {
  try {
    await apiFetch(`/api/v1/imports/${jobId}/mapping`, {
      method: "PUT",
      body: { mapping },
    });
    return { ok: true };
  } catch (err) {
    return failResult(err);
  }
}

export async function runImportDryRun(jobId: string): Promise<LoadState<ImportDryRunCounts>> {
  try {
    const result = await apiFetch<ImportDryRunDto>(`/api/v1/imports/${jobId}/validate`, {
      method: "POST",
    });
    return {
      status: "success",
      data: {
        create: result.wouldCreate,
        update: result.wouldUpdate,
        skip: result.wouldSkip,
        error: result.errorCount,
      },
    };
  } catch (err) {
    return { status: "error", error: toLoadError(err) };
  }
}

export async function commitImport(
  jobId: string,
  auditReason: string
): Promise<Result> {
  try {
    await apiFetch(`/api/v1/imports/${jobId}/commit`, {
      method: "POST",
      body: { auditReason },
    });
    return { ok: true };
  } catch (err) {
    return failResult(err);
  }
}

export async function fetchImportErrors(
  jobId: string
): Promise<Result<{ rows: { rowNumber: number; field: string; code: string; message: string }[] }>> {
  try {
    const rows = await apiFetch<
      { rowNumber: number; field: string; code: string; message: string }[]
    >(`/api/v1/imports/${jobId}/errors`);
    return { ok: true, rows };
  } catch (err) {
    return failResult(err);
  }
}

/* ——— R-C-E-012 / 013 Preview & issue ——— */

export async function runBillingPreview(termId: string): Promise<LoadState<PreviewRun>> {
  try {
    const [preview, students, programs, terms] = await Promise.all([
      apiFetch<PreviewDto>("/api/v1/billing/preview-runs", {
        method: "POST",
        body: { termId },
      }),
      apiFetch<StudentDto[]>("/api/v1/academic/students").catch(() => [] as StudentDto[]),
      apiFetch<ProgramDto[]>("/api/v1/academic/programs").catch(() => [] as ProgramDto[]),
      apiFetch<TermDto[]>("/api/v1/academic/terms").catch(() => [] as TermDto[]),
    ]);

    if (preview.studentCount === 0 || preview.results.length === 0) {
      return { status: "empty" };
    }

    const nameById = new Map(students.map((s) => [s.id, s.displayName]));
    const programByStudent = new Map(
      students.map((s) => [
        s.id,
        programs.find((p) => p.id === s.programId)?.code ?? "—",
      ])
    );
    const term = terms.find((t) => t.id === preview.termId);

    return {
      status: "success",
      data: {
        id: preview.previewRunId,
        termId: preview.termId,
        termLabel: term?.name ?? termId,
        totalAmount: preview.totalAmount,
        lineCount: preview.studentCount,
        outlierCount: preview.outlierCount,
        lines: preview.results.map((r) => ({
          studentNumber: r.studentExternalId,
          studentName: nameById.get(r.studentId) ?? r.studentExternalId,
          programCode: programByStudent.get(r.studentId) ?? "—",
          amount: r.totalAmount,
          isOutlier: r.isOutlier,
          outlierReason: r.outlierReason ?? undefined,
          invoiceStatus: "Draft" as const,
        })),
      },
    };
  } catch (err) {
    return { status: "error", error: toLoadError(err) };
  }
}

export async function issueStatements(input: {
  previewId: string;
  reason: string;
  termId?: string;
}): Promise<Result<{ issuedCount: number }>> {
  try {
    const result = await apiFetch<IssueDto>("/api/v1/billing/statements/issue", {
      method: "POST",
      body: {
        previewRunId: input.previewId,
        termId: input.termId ?? null,
        reason: input.reason,
        dueDate: null,
        instalmentPlanId: null,
      },
    });
    return { ok: true, issuedCount: result.issuedCount };
  } catch (err) {
    return failResult(err);
  }
}

/* ——— R-C-E-014 Adjustments ——— */

export async function submitAdjustment(input: AdjustmentInput): Promise<Result> {
  try {
    await apiFetch("/api/v1/billing/adjustments", {
      method: "POST",
      body: {
        invoiceId: input.invoiceId,
        reason: input.reason,
        lines: [
          {
            feeHeadCode: input.feeHeadCode,
            amount: input.amount,
            trace: `Portal adjustment ${input.feeHeadCode}`,
          },
        ],
      },
    });
    return { ok: true };
  } catch (err) {
    return failResult(err);
  }
}

export { ApiRequestError };
