/**
 * Portal data client. Live P1 API by default; set NEXT_PUBLIC_PORTAL_DATA=mock for mocks.
 */

import type { ApiErrorEnvelope } from "@fee/api-client";
import * as mock from "../mock/client";
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
import { useMockData } from "./config";
import type { AcademicTerm, AdjustmentInput, DiscountCategory } from "./live";
import * as live from "./live";

export type { AcademicTerm, AdjustmentInput, DiscountCategory };

export async function fetchBranding(): Promise<LoadState<BrandingConfig>> {
  return useMockData() ? mock.fetchBranding() : live.fetchBranding();
}

export async function saveBranding(
  patch: Pick<BrandingConfig, "tenantName"> & { brandColourHex?: string }
): Promise<{ ok: true } | { ok: false; error: ApiErrorEnvelope }> {
  return useMockData() ? mock.saveBranding(patch) : live.saveBranding(patch);
}

export async function fetchPrograms(): Promise<LoadState<Program[]>> {
  return useMockData() ? mock.fetchPrograms() : live.fetchPrograms();
}

export async function createProgram(input: {
  code: string;
  name: string;
  intakeLabel: string;
}): Promise<{ ok: true; id: string } | { ok: false; error: ApiErrorEnvelope }> {
  return useMockData() ? mock.createProgram(input) : live.createProgram(input);
}

export async function fetchFeeHeads(): Promise<LoadState<FeeHead[]>> {
  return useMockData() ? mock.fetchFeeHeads() : live.fetchFeeHeads();
}

export async function createFeeHead(input: {
  code: string;
  name: string;
}): Promise<{ ok: true; id: string } | { ok: false; error: ApiErrorEnvelope }> {
  return useMockData() ? mock.createFeeHead(input) : live.createFeeHead(input);
}

export async function fetchFeeSchedules(): Promise<LoadState<FeeScheduleVersion[]>> {
  return useMockData() ? mock.fetchFeeSchedules() : live.fetchFeeSchedules();
}

export async function fetchDiscountCategories(): Promise<LoadState<DiscountCategory[]>> {
  if (useMockData()) {
    return { status: "empty" };
  }
  return live.fetchDiscountCategories();
}

export async function createDiscountCategory(input: {
  name: string;
}): Promise<{ ok: true; id: string } | { ok: false; error: ApiErrorEnvelope }> {
  if (useMockData()) {
    return { ok: true, id: `disc-mock-${input.name}` };
  }
  return live.createDiscountCategory(input);
}

export async function fetchInstalmentPlans(): Promise<LoadState<InstalmentPlan[]>> {
  return useMockData() ? mock.fetchInstalmentPlans() : live.fetchInstalmentPlans();
}

export async function searchStudents(query: string): Promise<LoadState<StudentRow[]>> {
  return useMockData() ? mock.searchStudents(query) : live.searchStudents(query);
}

export async function fetchTerms(): Promise<LoadState<AcademicTerm[]>> {
  if (useMockData()) {
    return {
      status: "success",
      data: [{ id: "term-mock-fall-2026", code: "FALL-2026", name: "Fall 2026" }],
    };
  }
  return live.fetchTerms();
}

export async function uploadImport(input: {
  entity: string;
  file: File;
}): Promise<{ ok: true; jobId: string } | { ok: false; error: ApiErrorEnvelope }> {
  if (useMockData()) {
    return { ok: true, jobId: "job-mock-1" };
  }
  return live.uploadImport(input);
}

export async function setImportMapping(
  jobId: string,
  mapping: Record<string, string>
): Promise<{ ok: true } | { ok: false; error: ApiErrorEnvelope }> {
  if (useMockData()) {
    void jobId;
    void mapping;
    return { ok: true };
  }
  return live.setImportMapping(jobId, mapping);
}

export async function runImportDryRun(
  jobId: string
): Promise<LoadState<ImportDryRunCounts>> {
  if (useMockData()) {
    void jobId;
    return mock.runImportDryRun();
  }
  return live.runImportDryRun(jobId);
}

export async function commitImport(
  jobId: string,
  auditReason: string
): Promise<{ ok: true } | { ok: false; error: ApiErrorEnvelope }> {
  if (useMockData()) {
    void jobId;
    void auditReason;
    return mock.commitImport();
  }
  return live.commitImport(jobId, auditReason);
}

export async function fetchImportErrors(
  jobId: string
): Promise<
  | { ok: true; rows: { rowNumber: number; field: string; code: string; message: string }[] }
  | { ok: false; error: ApiErrorEnvelope }
> {
  if (useMockData()) {
    void jobId;
    return {
      ok: true,
      rows: [
        {
          rowNumber: 4,
          field: "external_id",
          code: "REQUIRED",
          message: "external_id is required (mock).",
        },
      ],
    };
  }
  return live.fetchImportErrors(jobId);
}

export async function runBillingPreview(
  termId: string
): Promise<LoadState<PreviewRun>> {
  if (useMockData()) {
    return mock.runBillingPreview(termId);
  }
  return live.runBillingPreview(termId);
}

export async function issueStatements(input: {
  previewId: string;
  reason: string;
  termId?: string;
}): Promise<{ ok: true; issuedCount: number } | { ok: false; error: ApiErrorEnvelope }> {
  return useMockData() ? mock.issueStatements(input) : live.issueStatements(input);
}

export async function submitAdjustment(
  input: AdjustmentInput
): Promise<{ ok: true } | { ok: false; error: ApiErrorEnvelope }> {
  if (useMockData()) {
    void input;
    return { ok: true };
  }
  return live.submitAdjustment(input);
}

export { useMockData } from "./config";
