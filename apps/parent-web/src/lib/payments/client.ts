/**
 * Parent payments client — typed against Agent D paths.
 * Default: mock when NEXT_PUBLIC_PARENT_DATA=mock (or unset).
 * See docs/supervisor/REQUESTS_TO_D.md for response-shape assumptions.
 */

import type { ApiErrorEnvelope } from "@fee/api-client";
import { delay, mockApiError } from "../mock/data";
import type { LoadState } from "../types";
import {
  buildMockPayContext,
  createMockAttempt,
  createMockPayLink,
  getMockPayOnBehalf,
  getStoredAttempt,
  getStoredPayLink,
  getStoredReceipt,
  mockHistoryGroups,
  mockPaymentError,
  pollMockAttempt,
  revokeMockPayLink,
} from "./mock-data";
import type {
  CreatePayLinkRequest,
  CreatePaymentAttemptRequest,
  PayContext,
  PayLink,
  PayOnBehalfPreview,
  PaymentAttempt,
  PaymentChannel,
  PaymentHistoryGroup,
  PaymentReceipt,
  PaymentsResult,
} from "./types";

function useMock(): boolean {
  const mode = process.env.NEXT_PUBLIC_PARENT_DATA ?? "mock";
  return mode === "mock";
}

function apiBase(): string {
  return process.env.NEXT_PUBLIC_API_BASE_URL ?? "";
}

async function parseError(res: Response): Promise<ApiErrorEnvelope> {
  try {
    const body = (await res.json()) as ApiErrorEnvelope;
    if (body?.error?.message) return body;
  } catch {
    /* fall through */
  }
  return {
    error: {
      code: "UNKNOWN_ERROR",
      message: "Something went wrong. Please try again.",
      messageKey: "errors.unknown",
      correlationId: "00000000-0000-0000-0000-000000000000",
      details: [],
      retryable: true,
    },
  };
}

async function getJson<T>(path: string): Promise<PaymentsResult<T>> {
  try {
    const res = await fetch(`${apiBase()}${path}`, {
      credentials: "include",
      headers: { Accept: "application/json" },
    });
    if (!res.ok) return { ok: false, error: await parseError(res) };
    return { ok: true, data: (await res.json()) as T };
  } catch {
    return { ok: false, error: mockApiError };
  }
}

async function postJson<T>(
  path: string,
  body: unknown,
  idempotencyKey?: string
): Promise<PaymentsResult<T>> {
  try {
    const headers: Record<string, string> = {
      Accept: "application/json",
      "Content-Type": "application/json",
    };
    if (idempotencyKey) headers["Idempotency-Key"] = idempotencyKey;
    const res = await fetch(`${apiBase()}${path}`, {
      method: "POST",
      credentials: "include",
      headers,
      body: JSON.stringify(body),
    });
    if (!res.ok) return { ok: false, error: await parseError(res) };
    return { ok: true, data: (await res.json()) as T };
  } catch {
    return { ok: false, error: mockApiError };
  }
}

/** Highest enabled tier first; within tier, stable by name. */
export function sortChannelsByTier(
  channels: PaymentChannel[]
): PaymentChannel[] {
  return [...channels].sort((a, b) => {
    if (a.tier !== b.tier) return a.tier - b.tier;
    return a.name.localeCompare(b.name);
  });
}

export function pickRecommendedChannel(
  channels: PaymentChannel[]
): PaymentChannel | null {
  const sorted = sortChannelsByTier(channels);
  const tier1 = sorted.find((c) => c.tier === 1);
  if (tier1) return tier1;
  return sorted[0] ?? null;
}

export async function fetchPayContext(
  studentId: string
): Promise<LoadState<PayContext>> {
  if (useMock()) {
    await delay(null, 400);
    const ctx = buildMockPayContext(studentId);
    if (!ctx) return { status: "error", error: mockPaymentError };
    if (ctx.fullBalanceAmount === "0.00" && ctx.instalmentAmount === "0.00") {
      return { status: "empty" };
    }
    return { status: "success", data: ctx };
  }

  const channelsRes = await getJson<{ items: PaymentChannel[] }>(
    `/api/v1/payments/channels?studentId=${encodeURIComponent(studentId)}`
  );
  if (!channelsRes.ok) return { status: "error", error: channelsRes.error };

  // Statement-shaped context until D returns a combined pay bootstrap.
  // See REQUESTS_TO_D.md R-D-F-006.
  const ctx = buildMockPayContext(studentId);
  if (!ctx) return { status: "error", error: mockPaymentError };
  return {
    status: "success",
    data: { ...ctx, channels: channelsRes.data.items },
  };
}

export async function createPaymentAttempt(
  input: CreatePaymentAttemptRequest
): Promise<PaymentsResult<PaymentAttempt>> {
  if (useMock()) {
    await delay(null, 500);
    const attempt = createMockAttempt({
      studentId: input.studentId,
      amount: input.amount,
      channelId: input.channelId,
    });
    if (!attempt) return { ok: false, error: mockPaymentError };
    return { ok: true, data: attempt };
  }

  return postJson<PaymentAttempt>(
    "/api/v1/payments/attempts",
    {
      studentId: input.studentId,
      amount: input.amount,
      selection: input.selection,
      instalmentId: input.instalmentId,
      channelId: input.channelId,
    },
    input.idempotencyKey
  );
}

export async function fetchPaymentAttempt(
  attemptId: string
): Promise<LoadState<PaymentAttempt>> {
  if (useMock()) {
    await delay(null, 350);
    // Demo modes via query are handled in the screen; store / poll here.
    let attempt = getStoredAttempt(attemptId) ?? null;
    if (!attempt) {
      // Synthesize from id prefixes for deep-link demos
      if (attemptId.startsWith("demo-pending")) {
        attempt = createMockAttempt({
          studentId: "stu-hamza",
          amount: "71250.00",
          channelId: "ch-jazzcash",
          attemptId,
          forceStatus: "Pending",
        });
      } else if (attemptId.startsWith("demo-failed")) {
        attempt = createMockAttempt({
          studentId: "stu-hamza",
          amount: "71250.00",
          channelId: "ch-card",
          attemptId,
          forceStatus: "Failed",
        });
      } else if (attemptId.startsWith("demo-expired")) {
        attempt = createMockAttempt({
          studentId: "stu-hamza",
          amount: "71250.00",
          channelId: "ch-card",
          attemptId,
          forceStatus: "Expired",
        });
      } else if (attemptId.startsWith("demo-voucher")) {
        attempt = createMockAttempt({
          studentId: "stu-hamza",
          amount: "71250.00",
          channelId: "ch-voucher",
          attemptId,
          forceStatus: "Pending",
        });
      } else if (attemptId.startsWith("demo-succeeded")) {
        attempt = createMockAttempt({
          studentId: "stu-hamza",
          amount: "71250.00",
          channelId: "ch-raast",
          attemptId,
          forceStatus: "Succeeded",
        });
      }
    } else if (attempt.status === "Pending") {
      attempt = pollMockAttempt(attemptId) ?? attempt;
    }
    if (!attempt) return { status: "error", error: mockPaymentError };
    return { status: "success", data: attempt };
  }

  const res = await getJson<PaymentAttempt>(
    `/api/v1/payments/attempts/${encodeURIComponent(attemptId)}`
  );
  if (!res.ok) return { status: "error", error: res.error };
  return { status: "success", data: res.data };
}

export async function fetchPaymentReceipt(
  receiptId: string
): Promise<LoadState<PaymentReceipt>> {
  if (useMock()) {
    await delay(null, 350);
    let receipt = getStoredReceipt(receiptId);
    if (!receipt && receiptId.startsWith("demo-")) {
      const attempt = createMockAttempt({
        studentId: "stu-hamza",
        amount: "71250.00",
        channelId: "ch-raast",
        attemptId: `att-for-${receiptId}`,
        forceStatus: "Succeeded",
      });
      if (attempt?.receiptId) {
        receipt = getStoredReceipt(attempt.receiptId);
      }
    }
    if (!receipt) return { status: "error", error: mockPaymentError };
    return { status: "success", data: receipt };
  }

  const res = await getJson<PaymentReceipt>(
    `/api/v1/payments/receipts/${encodeURIComponent(receiptId)}`
  );
  if (!res.ok) return { status: "error", error: res.error };
  return { status: "success", data: res.data };
}

export async function fetchPaymentHistory(opts?: {
  studentName?: string;
}): Promise<LoadState<PaymentHistoryGroup[]>> {
  const studentName = opts?.studentName?.trim();

  if (useMock()) {
    await delay(null, 400);
    const groups = mockHistoryGroups(studentName);
    if (groups.length === 0) return { status: "empty" };
    return { status: "success", data: groups };
  }

  // Proposed path — filed in REQUESTS_TO_D.md R-D-F-001
  const res = await getJson<{ groups: PaymentHistoryGroup[] }>(
    "/api/v1/payments/history"
  );
  // Live history API is not shipped yet — fall back to demo history filtered
  // to this parent's linked student only.
  if (!res.ok) {
    const groups = mockHistoryGroups(studentName);
    if (groups.length === 0) return { status: "empty" };
    return { status: "success", data: groups };
  }

  let groups = res.data.groups;
  if (studentName) {
    const target = studentName.toLowerCase();
    groups = groups
      .map((g) => ({
        ...g,
        items: g.items.filter(
          (item) => item.studentName.trim().toLowerCase() === target
        ),
      }))
      .filter((g) => g.items.length > 0);
  }
  if (!groups.length) return { status: "empty" };
  return { status: "success", data: groups };
}

export async function createPayLink(
  input: CreatePayLinkRequest
): Promise<PaymentsResult<PayLink>> {
  if (useMock()) {
    await delay(null, 400);
    const link = createMockPayLink(input.studentId, input.amount);
    if (!link) return { ok: false, error: mockPaymentError };
    return { ok: true, data: link };
  }

  return postJson<PayLink>("/api/v1/payments/pay-links", input);
}

export async function fetchPayOnBehalf(
  token: string
): Promise<LoadState<PayOnBehalfPreview>> {
  if (useMock()) {
    await delay(null, 350);
    const preview = getMockPayOnBehalf(token);
    if (!preview) return { status: "error", error: mockPaymentError };
    return { status: "success", data: preview };
  }

  // Proposed path — filed in REQUESTS_TO_D.md R-D-F-002
  const res = await getJson<PayOnBehalfPreview>(
    `/api/v1/payments/pay-links/${encodeURIComponent(token)}`
  );
  if (!res.ok) return { status: "error", error: res.error };
  return { status: "success", data: res.data };
}

export async function revokePayLink(
  token: string
): Promise<PaymentsResult<{ revoked: true }>> {
  if (useMock()) {
    await delay(null, 300);
    const ok = revokeMockPayLink(token);
    if (!ok) {
      const link = getStoredPayLink(token);
      if (!link) return { ok: false, error: mockPaymentError };
    }
    return { ok: true, data: { revoked: true } };
  }

  // Proposed — REQUESTS_TO_D.md R-D-F-003
  return postJson(`/api/v1/payments/pay-links/${encodeURIComponent(token)}/revoke`, {});
}
