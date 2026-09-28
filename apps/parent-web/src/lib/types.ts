import type { ApiErrorEnvelope } from "@fee/api-client";
import type { StatusPillKind } from "@fee/ui";

/** Local UI types until C publishes parent OpenAPI paths — see REQUESTS_TO_C_FROM_F.md */

export type LoadState<T> =
  | { status: "loading" }
  | { status: "empty" }
  | { status: "error"; error: ApiErrorEnvelope }
  | { status: "success"; data: T };

export type InvoiceStatus = "Draft" | "Issued" | "PartiallyPaid" | "Paid" | "Superseded";
export type ClearanceStatus = "Cleared" | "ClearedWithDeferment" | "NotCleared";
export type LineKind = "Charge" | "Discount" | "Adjustment" | "LateFee";

export type ParentProfile = {
  id: string;
  displayName: string;
  mobileE164: string;
};

export type LinkedStudent = {
  id: string;
  fullName: string;
  programLabel: string;
  termLabel: string;
  institutionId: string;
  institutionName: string;
  /** Identity chip only — never used for action colours. */
  tenantBrandCss: string;
  amountDue: string;
  dueDate: string;
  invoiceStatus: InvoiceStatus;
  isOverdue: boolean;
  clearanceStatus: ClearanceStatus;
  clearanceCutoffDate?: string;
};

export type InstitutionGroup = {
  institutionId: string;
  institutionName: string;
  tenantBrandCss: string;
  students: LinkedStudent[];
};

export type FamilyHome = {
  parent: ParentProfile;
  totalDue: string;
  nextDue: {
    studentId: string;
    studentName: string;
    dueDate: string;
    amount: string;
    institutionName: string;
  } | null;
  groups: InstitutionGroup[];
  clearanceAlerts: Array<{
    studentId: string;
    studentName: string;
    amountDue: string;
    cutoffDate: string;
  }>;
  firstVisitLinkedCount?: number;
};

export type StatementLine = {
  id: string;
  feeHeadName: string;
  amount: string;
  kind: LineKind;
  /** Human trace only — never show rule ids (CONTRACT §7.1). */
  trace?: string;
};

export type TermDelta = {
  feeHeadName: string;
  percentChange: number;
  direction: "up" | "down" | "flat";
};

export type InstalmentRow = {
  id: string;
  sequence: number;
  dueDate: string;
  amount: string;
  status: InvoiceStatus;
  isOverdue: boolean;
  payable: boolean;
};

export type StudentStatement = {
  studentId: string;
  studentName: string;
  programLabel: string;
  termLabel: string;
  institutionName: string;
  tenantBrandCss: string;
  amountDue: string;
  dueDate: string;
  invoiceStatus: InvoiceStatus;
  isOverdue: boolean;
  clearanceStatus: ClearanceStatus;
  updatedBannerDate?: string;
  termDiff: {
    totalPercentChange: number;
    lines: TermDelta[];
  };
  lines: StatementLine[];
  instalments: InstalmentRow[];
  nextPayableAmount: string;
};

export type DisplayInvoiceKind = Extract<
  StatusPillKind,
  "Paid" | "PartiallyPaid" | "Issued" | "DueSoon" | "Overdue"
>;
