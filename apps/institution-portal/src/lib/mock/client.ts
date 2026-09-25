/**
 * Mock portal data client.
 * TODO(Agent C): replace with @fee/api-client calls once OpenAPI paths land.
 * See docs/supervisor/REQUESTS_TO_C.md
 */

import type { ApiErrorEnvelope } from "@fee/api-client";
import {
  delay,
  mockApiError,
  mockBranding,
  mockFeeHeads,
  mockInstalmentPlans,
  mockPreviewRun,
  mockPrograms,
  mockSchedules,
  mockStudents,
} from "./data";
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

export type MockMode = "success" | "empty" | "error";

let listMode: MockMode = "success";

export function setMockListMode(mode: MockMode) {
  listMode = mode;
}

export function getMockListMode(): MockMode {
  return listMode;
}

async function wrapList<T>(items: T[]): Promise<LoadState<T[]>> {
  await delay(null);
  if (listMode === "error") return { status: "error", error: mockApiError };
  if (listMode === "empty" || items.length === 0) return { status: "empty" };
  return { status: "success", data: items };
}

export async function fetchBranding(): Promise<LoadState<BrandingConfig>> {
  await delay(null);
  if (listMode === "error") return { status: "error", error: mockApiError };
  return { status: "success", data: mockBranding };
}

export async function saveBranding(
  patch: Pick<BrandingConfig, "tenantName">
): Promise<{ ok: true } | { ok: false; error: ApiErrorEnvelope }> {
  await delay(null, 200);
  // TODO: POST /api/v1/tenants/{tenantId}/branding
  void patch;
  return { ok: true };
}

export async function fetchPrograms(): Promise<LoadState<Program[]>> {
  return wrapList(mockPrograms);
}

export async function createProgram(input: {
  code: string;
  name: string;
  intakeLabel: string;
}): Promise<{ ok: true; id: string } | { ok: false; error: ApiErrorEnvelope }> {
  await delay(null, 200);
  // TODO: POST /api/v1/academic/programs
  return { ok: true, id: `prog-mock-${input.code}` };
}

export async function fetchFeeHeads(): Promise<LoadState<FeeHead[]>> {
  return wrapList(mockFeeHeads);
}

export async function createFeeHead(input: {
  code: string;
  name: string;
}): Promise<{ ok: true; id: string } | { ok: false; error: ApiErrorEnvelope }> {
  await delay(null, 200);
  // TODO: POST /api/v1/billing/fee-heads
  return { ok: true, id: `fh-mock-${input.code}` };
}

export async function fetchFeeSchedules(): Promise<
  LoadState<FeeScheduleVersion[]>
> {
  return wrapList(mockSchedules);
}

export async function fetchInstalmentPlans(): Promise<
  LoadState<InstalmentPlan[]>
> {
  return wrapList(mockInstalmentPlans);
}

export async function searchStudents(
  query: string
): Promise<LoadState<StudentRow[]>> {
  await delay(null);
  if (listMode === "error") return { status: "error", error: mockApiError };
  const q = query.trim().toLowerCase();
  const filtered = q
    ? mockStudents.filter(
        (s) =>
          s.fullName.toLowerCase().includes(q) ||
          s.studentNumber.toLowerCase().includes(q)
      )
    : mockStudents;
  if (filtered.length === 0) return { status: "empty" };
  return { status: "success", data: filtered };
}

export async function runImportDryRun(): Promise<
  LoadState<ImportDryRunCounts>
> {
  await delay(null, 600);
  if (listMode === "error") return { status: "error", error: mockApiError };
  return {
    status: "success",
    data: { create: 120, update: 18, skip: 4, error: 2 },
  };
}

export async function commitImport(): Promise<
  { ok: true } | { ok: false; error: ApiErrorEnvelope }
> {
  await delay(null, 400);
  // TODO: POST /api/v1/imports/{jobId}/commit
  return { ok: true };
}

export async function runBillingPreview(termLabel: string): Promise<
  LoadState<PreviewRun>
> {
  await delay(null, 700);
  if (listMode === "error") return { status: "error", error: mockApiError };
  return {
    status: "success",
    data: { ...mockPreviewRun, termLabel },
  };
}

export async function issueStatements(input: {
  previewId: string;
  reason: string;
}): Promise<{ ok: true; issuedCount: number } | { ok: false; error: ApiErrorEnvelope }> {
  await delay(null, 500);
  // TODO: POST /api/v1/billing/statements/issue
  void input;
  return { ok: true, issuedCount: mockPreviewRun.lineCount };
}
