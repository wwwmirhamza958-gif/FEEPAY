import type { ApiErrorEnvelope } from "@fee/api-client";

/** Local UI types until C publishes OpenAPI paths — do not invent EF schemas. */

export type LoadState<T> =
  | { status: "loading" }
  | { status: "empty" }
  | { status: "error"; error: ApiErrorEnvelope }
  | { status: "success"; data: T };

export type Program = {
  id: string;
  code: string;
  name: string;
  intakeLabel: string;
};

export type FeeHead = {
  id: string;
  code: string;
  name: string;
  taxableFor236I: boolean;
};

export type FeeScheduleVersion = {
  id: string;
  name: string;
  version: number;
  effectiveFrom: string;
  status: "Draft" | "Published";
};

export type InstalmentPlan = {
  id: string;
  name: string;
  parts: number;
  note: string;
};

export type BrandingConfig = {
  tenantName: string;
  brandColourTokenNote: string;
  /** Identity only — never used for buttons (§3.2). */
  brandColourCssVar: string;
  /** From API branding payload — display only, never as a button fill. */
  brandColourHex?: string;
};

export type StudentRow = {
  id: string;
  studentNumber: string;
  fullName: string;
  programCode: string;
  intakeLabel: string;
};

/** CONTRACT §2.4 Import job */
export type ImportJobStatus =
  | "Uploaded"
  | "Validating"
  | "Validated"
  | "FailedValidation"
  | "Committing"
  | "Succeeded"
  | "Failed"
  | "Reverted";

export type ImportDryRunCounts = {
  create: number;
  update: number;
  skip: number;
  error: number;
};

export type PreviewLine = {
  studentNumber: string;
  studentName: string;
  programCode: string;
  amount: string;
  isOutlier: boolean;
  outlierReason?: string;
  invoiceStatus: "Draft";
};

export type PreviewRun = {
  id: string;
  termId?: string;
  termLabel: string;
  totalAmount: string;
  lineCount: number;
  outlierCount: number;
  lines: PreviewLine[];
};
