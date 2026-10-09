import type { InvoiceStatus } from "../types";

/** Single-student guardian dashboard (not multi-child family home). */
export type GuardianFeeDashboard = {
  guardianDisplayName: string;
  mobileE164: string;
  studentId: string;
  studentName: string;
  studentExternalId: string;
  classLabel: string;
  tenantId: string;
  institutionName: string;
  tenantBrandCss: string;
  invoiceId: string | null;
  feeAmount: string;
  dueDate: string;
  paymentStatus: InvoiceStatus | string;
  canPay: boolean;
  isOverdue: boolean;
};

export type ParentPaymentChannel = {
  adapterCode: string;
  displayName: string;
  tier: 1 | 2 | 3;
  meta: string;
  handoffLabel: string;
};

export type ParentPayContext = {
  fee: GuardianFeeDashboard;
  payableType: string;
  payableId: string;
  channels: ParentPaymentChannel[];
};
