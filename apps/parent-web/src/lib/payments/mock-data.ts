import type { ApiErrorEnvelope } from "@fee/api-client";
import { mockStatements } from "../mock/data";
import type {
  PayContext,
  PayLink,
  PayOnBehalfPreview,
  PaymentAttempt,
  PaymentChannel,
  PaymentHistoryGroup,
  PaymentHistoryItem,
  PaymentReceipt,
} from "./types";

export const mockPaymentError: ApiErrorEnvelope = {
  error: {
    code: "RESOURCE_NOT_FOUND",
    message: "We couldn't load payment details. Check your connection and try again.",
    messageKey: "errors.resource_not_found",
    correlationId: "f0000100-aaaa-bbbb-cccc-dddddddddddd",
    details: [],
    retryable: true,
  },
};

export const mockChannels: PaymentChannel[] = [
  {
    id: "ch-raast",
    code: "raast",
    name: "Raast",
    tier: 1,
    meta: "Instant · No extra charge",
    handoffLabel: "Opening Raast…",
    kind: "instant",
  },
  {
    id: "ch-jazzcash",
    code: "jazzcash",
    name: "JazzCash",
    tier: 1,
    meta: "Wallet · Opens JazzCash",
    handoffLabel: "Opening JazzCash…",
    kind: "wallet",
  },
  {
    id: "ch-easypaisa",
    code: "easypaisa",
    name: "Easypaisa",
    tier: 1,
    meta: "Wallet · Opens Easypaisa",
    handoffLabel: "Opening Easypaisa…",
    kind: "wallet",
  },
  {
    id: "ch-card",
    code: "card",
    name: "Card",
    tier: 2,
    meta: "Card · Opens secure checkout · Bank fee may apply",
    handoffLabel: "Opening secure checkout…",
    kind: "card_hosted",
  },
  {
    id: "ch-voucher",
    code: "voucher",
    name: "Bank voucher",
    tier: 3,
    meta: "Bank voucher · Pay at any bank app, ATM or branch",
    handoffLabel: "Preparing your consumer number…",
    kind: "voucher",
  },
];

const attemptStore = new Map<string, PaymentAttempt>();
const receiptStore = new Map<string, PaymentReceipt>();
const payLinkStore = new Map<string, PayLink>();
const pollTicks = new Map<string, number>();
let seedReady = false;

export function resetPaymentMockStores() {
  attemptStore.clear();
  receiptStore.clear();
  payLinkStore.clear();
  pollTicks.clear();
  seedReady = false;
}

function initialsFromName(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) return "?";
  if (parts.length === 1) return parts[0]!.slice(0, 2).toUpperCase();
  return `${parts[0]![0] ?? ""}${parts[parts.length - 1]![0] ?? ""}`.toUpperCase();
}

export function buildMockPayContext(studentId: string): PayContext | null {
  const statement = mockStatements[studentId];
  if (!statement) return null;
  const payable = statement.instalments.find((i) => i.payable);
  return {
    studentId: statement.studentId,
    studentName: statement.studentName,
    institutionName: statement.institutionName,
    tenantBrandCss: statement.tenantBrandCss,
    termLabel: statement.termLabel,
    dueDate: statement.dueDate,
    instalmentAmount: payable?.amount ?? statement.nextPayableAmount,
    instalmentId: payable?.id ?? null,
    fullBalanceAmount: statement.amountDue,
    partialPaymentAllowed: false,
    channels: mockChannels,
  };
}

export function getStoredAttempt(id: string): PaymentAttempt | undefined {
  return attemptStore.get(id);
}

export function getStoredReceipt(id: string): PaymentReceipt | undefined {
  return receiptStore.get(id);
}

export function getStoredPayLink(token: string): PayLink | undefined {
  return payLinkStore.get(token);
}

function createMockReceiptForAttempt(attempt: PaymentAttempt): PaymentReceipt {
  const id = `rcpt-${attempt.id}`;
  const existing = receiptStore.get(id);
  if (existing) return existing;
  const receipt: PaymentReceipt = {
    id,
    receiptNumber: `PU-2026-${String(receiptStore.size + 1001).padStart(5, "0")}`,
    amount: attempt.amount,
    paidAt: attempt.createdAt,
    channelName: attempt.channelName,
    gatewayReference: `GW-${attempt.id.slice(-8).toUpperCase()}`,
    institutionName: attempt.institutionName,
    tenantBrandCss: attempt.tenantBrandCss,
    studentName: attempt.studentName,
    termLabel: attempt.termLabel,
    lines: [{ feeHeadName: "Tuition (instalment)", amount: attempt.amount }],
    qrPayload: `https://verify.example.test/r/${id}`,
    pdfUrl: null,
  };
  receiptStore.set(id, receipt);
  return receipt;
}

export function createMockAttempt(input: {
  studentId: string;
  amount: string;
  channelId: string;
  attemptId?: string;
  forceStatus?: PaymentAttempt["status"];
}): PaymentAttempt | null {
  const ctx = buildMockPayContext(input.studentId);
  if (!ctx) return null;
  const channel =
    mockChannels.find((c) => c.id === input.channelId) ?? mockChannels[0]!;
  const id =
    input.attemptId ??
    `att-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
  const isVoucher = channel.kind === "voucher";
  const status = input.forceStatus ?? "Pending";

  const attempt: PaymentAttempt = {
    id,
    status,
    amount: input.amount,
    channelId: channel.id,
    channelCode: channel.code,
    channelName: channel.name,
    studentId: ctx.studentId,
    studentName: ctx.studentName,
    institutionName: ctx.institutionName,
    tenantBrandCss: ctx.tenantBrandCss,
    termLabel: ctx.termLabel,
    createdAt: new Date().toISOString(),
    redirectUrl: isVoucher
      ? null
      : `https://checkout.example.test/mock/${channel.code}?attempt=${id}`,
    voucherConsumerNumber: isVoucher ? "1234567890123" : null,
    receiptId: null,
    failureReason: null,
    moneyTaken: false,
  };

  if (status === "Failed") {
    attempt.failureReason =
      "Your bank declined this payment. No money was taken.";
    attempt.moneyTaken = false;
    attempt.redirectUrl = null;
  }
  if (status === "Expired") {
    attempt.failureReason =
      "This payment session expired. No money was taken.";
    attempt.moneyTaken = false;
    attempt.redirectUrl = null;
  }
  if (status === "Succeeded") {
    const receipt = createMockReceiptForAttempt(attempt);
    attempt.receiptId = receipt.id;
    attempt.redirectUrl = null;
  }

  attemptStore.set(id, attempt);
  pollTicks.set(id, 0);
  return attempt;
}

/** Advance mock pending → succeeded after a few polls. */
export function pollMockAttempt(id: string): PaymentAttempt | undefined {
  const attempt = attemptStore.get(id);
  if (!attempt) return undefined;
  if (attempt.status !== "Pending") return attempt;

  const ticks = (pollTicks.get(id) ?? 0) + 1;
  pollTicks.set(id, ticks);

  const threshold = attempt.channelCode === "voucher" ? 8 : 3;
  if (ticks >= threshold) {
    attempt.status = "Succeeded";
    const receipt = createMockReceiptForAttempt(attempt);
    attempt.receiptId = receipt.id;
    attempt.redirectUrl = null;
    attemptStore.set(id, attempt);
  }
  return attempt;
}

function ensureSeedHistory() {
  if (seedReady) return;
  seedReady = true;
  createMockAttempt({
    studentId: "stu-hamza",
    amount: "50625.00",
    channelId: "ch-raast",
    attemptId: "att-seed-1",
    forceStatus: "Succeeded",
  });
  createMockAttempt({
    studentId: "stu-sara",
    amount: "45000.00",
    channelId: "ch-jazzcash",
    attemptId: "att-seed-2",
    forceStatus: "Succeeded",
  });
}

export function mockHistoryGroups(): PaymentHistoryGroup[] {
  ensureSeedHistory();
  const items: PaymentHistoryItem[] = [...receiptStore.values()].map((r) => {
    const attempt = [...attemptStore.values()].find((a) => a.receiptId === r.id);
    return {
      id: attempt?.id ?? r.id,
      receiptId: r.id,
      amount: r.amount,
      paidAt: r.paidAt,
      studentName: r.studentName,
      channelName: r.channelName,
      status: "Succeeded",
      termLabel: r.termLabel,
    };
  });

  items.push({
    id: "att-seed-failed",
    receiptId: null,
    amount: "71250.00",
    paidAt: "2026-09-20T10:00:00.000Z",
    studentName: "Hamza Khan",
    channelName: "Card",
    status: "Failed",
    termLabel: "Fall 2026",
  });

  const byTerm = new Map<string, PaymentHistoryItem[]>();
  for (const item of items) {
    const list = byTerm.get(item.termLabel) ?? [];
    list.push(item);
    byTerm.set(item.termLabel, list);
  }

  return [...byTerm.entries()].map(([termLabel, termItems]) => ({
    termLabel,
    items: termItems.sort((a, b) => b.paidAt.localeCompare(a.paidAt)),
  }));
}

export function createMockPayLink(
  studentId: string,
  amount: string
): PayLink | null {
  const ctx = buildMockPayContext(studentId);
  if (!ctx) return null;
  const token = `pob-${studentId}-${Date.now().toString(36)}`;
  const preview: PayOnBehalfPreview = {
    token,
    institutionName: ctx.institutionName,
    tenantBrandCss: ctx.tenantBrandCss,
    studentInitials: initialsFromName(ctx.studentName),
    termLabel: ctx.termLabel,
    amount,
    dueDate: ctx.dueDate,
    requestedByLabel: "Requested by a family member",
  };
  const link: PayLink = {
    id: `link-${token}`,
    url:
      typeof window !== "undefined"
        ? `${window.location.origin}/pay-on-behalf/${token}`
        : `https://pay.example.test/pay-on-behalf/${token}`,
    expiresAt: "2026-10-20T23:59:59.000Z",
    preview,
    revoked: false,
  };
  payLinkStore.set(token, link);
  return link;
}

export function getMockPayOnBehalf(token: string): PayOnBehalfPreview | null {
  const existing = payLinkStore.get(token);
  if (existing && !existing.revoked) return existing.preview;
  if (token === "demo-hamza") {
    const ctx = buildMockPayContext("stu-hamza");
    if (!ctx) return null;
    return {
      token,
      institutionName: ctx.institutionName,
      tenantBrandCss: ctx.tenantBrandCss,
      studentInitials: initialsFromName(ctx.studentName),
      termLabel: ctx.termLabel,
      amount: ctx.instalmentAmount,
      dueDate: ctx.dueDate,
      requestedByLabel: "Requested by a family member",
    };
  }
  return null;
}

export function revokeMockPayLink(token: string): boolean {
  const link = payLinkStore.get(token);
  if (!link) return false;
  link.revoked = true;
  payLinkStore.set(token, link);
  return true;
}
