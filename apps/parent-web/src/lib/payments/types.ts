import type { ApiErrorEnvelope } from "@fee/api-client";

/** CONTRACT §2.1 PaymentAttempt statuses — do not invent synonyms. */
export type PaymentAttemptStatus =
  | "Created"
  | "Pending"
  | "Succeeded"
  | "Failed"
  | "Expired"
  | "Cancelled"
  | "Reversed";

export type PaymentChannelTier = 1 | 2 | 3;

export type PaymentChannelKind =
  | "instant"
  | "wallet"
  | "card_hosted"
  | "voucher";

/** GET /api/v1/payments/channels */
export type PaymentChannel = {
  id: string;
  code: string;
  name: string;
  tier: PaymentChannelTier;
  /** Honest meta line, e.g. "Instant · No extra charge". */
  meta: string;
  /** Interstitial copy before leave, e.g. "Opening JazzCash…". */
  handoffLabel: string;
  kind: PaymentChannelKind;
};

export type PaymentSelection = "instalment" | "full_balance";

export type CreatePaymentAttemptRequest = {
  studentId: string;
  amount: string;
  selection: PaymentSelection;
  instalmentId?: string;
  channelId: string;
  idempotencyKey: string;
};

/** POST /api/v1/payments/attempts + GET …/attempts/{id} */
export type PaymentAttempt = {
  id: string;
  status: PaymentAttemptStatus;
  amount: string;
  channelId: string;
  channelCode: string;
  channelName: string;
  studentId: string;
  studentName: string;
  institutionName: string;
  tenantBrandCss: string;
  termLabel: string;
  createdAt: string;
  /** System browser tab only — never WebView (plan §6.6). */
  redirectUrl?: string | null;
  voucherConsumerNumber?: string | null;
  receiptId?: string | null;
  /** Human reason for Failed / Expired. */
  failureReason?: string | null;
  /** When Failed/Expired, whether money was taken (API-confirmed). */
  moneyTaken: boolean;
};

export type ReceiptLine = {
  feeHeadName: string;
  amount: string;
};

/** GET /api/v1/payments/receipts/{id} */
export type PaymentReceipt = {
  id: string;
  receiptNumber: string;
  amount: string;
  paidAt: string;
  channelName: string;
  gatewayReference: string;
  institutionName: string;
  tenantBrandCss: string;
  studentName: string;
  termLabel: string;
  lines: ReceiptLine[];
  /** Public verification payload for QR (placeholder until D ships). */
  qrPayload: string;
  pdfUrl?: string | null;
};

export type PaymentHistoryItem = {
  id: string;
  receiptId?: string | null;
  amount: string;
  paidAt: string;
  studentName: string;
  channelName: string;
  status: PaymentAttemptStatus;
  termLabel: string;
};

export type PaymentHistoryGroup = {
  termLabel: string;
  items: PaymentHistoryItem[];
};

/** POST /api/v1/payments/pay-links */
export type CreatePayLinkRequest = {
  studentId: string;
  amount: string;
};

export type PayOnBehalfPreview = {
  token: string;
  institutionName: string;
  tenantBrandCss: string;
  studentInitials: string;
  termLabel: string;
  amount: string;
  dueDate: string;
  requestedByLabel: string;
};

export type PayLink = {
  id: string;
  url: string;
  expiresAt: string;
  preview: PayOnBehalfPreview;
  revoked: boolean;
};

/** Pay context for step flow (from statement + channels). */
export type PayContext = {
  studentId: string;
  studentName: string;
  institutionName: string;
  tenantBrandCss: string;
  termLabel: string;
  dueDate: string;
  instalmentAmount: string;
  instalmentId: string | null;
  fullBalanceAmount: string;
  partialPaymentAllowed: boolean;
  channels: PaymentChannel[];
};

export type PaymentsResult<T> =
  | { ok: true; data: T }
  | { ok: false; error: ApiErrorEnvelope };
