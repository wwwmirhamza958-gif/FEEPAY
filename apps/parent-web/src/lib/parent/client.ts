/**
 * Parent API client — live by default when NEXT_PUBLIC_PARENT_DATA=live.
 * Mock remains available for offline UI work.
 */

import type { ApiErrorEnvelope } from "@fee/api-client";
import {
  delay,
  MOCK_LINKED_MOBILE,
  MOCK_OTP_CODE,
  MOCK_RATE_LIMIT_MOBILE,
  MOCK_UNLINKED_MOBILE,
  mockApiError,
  mockNoStudents,
  mockOtpRateLimited,
  mockOtpWrong,
} from "../mock/data";
import type { LoadState } from "../types";
import type {
  GuardianFeeDashboard,
  ParentPayContext,
  ParentPaymentChannel,
} from "./types";

const SESSION_KEY = "fee.parent.session";

function useMock(): boolean {
  const mode = process.env.NEXT_PUBLIC_PARENT_DATA ?? "live";
  return mode === "mock";
}

/**
 * Browser calls go through `/api/platform` (Next rewrite) to avoid CORS.
 * NEXT_PUBLIC_API_BASE_URL is only the rewrite upstream, not the fetch origin.
 */
function apiBase(): string {
  if (typeof window !== "undefined") {
    return "/api/platform";
  }
  return (
    process.env.NEXT_PUBLIC_API_BASE_URL?.replace(/\/$/, "") ||
    "http://localhost:5137"
  );
}

export function digitsOnly(value: string): string {
  return value.replace(/\D/g, "");
}

export function getParentSessionId(): string | null {
  if (typeof window === "undefined") return null;
  return window.sessionStorage.getItem(SESSION_KEY);
}

export function setParentSessionId(token: string) {
  if (typeof window === "undefined") return;
  window.sessionStorage.setItem(SESSION_KEY, token);
}

export function clearParentSession() {
  if (typeof window === "undefined") return;
  window.sessionStorage.removeItem(SESSION_KEY);
}

async function parseError(res: Response): Promise<ApiErrorEnvelope> {
  try {
    const body = (await res.json()) as ApiErrorEnvelope;
    if (body?.error?.message) return body;
  } catch {
    /* fall through */
  }
  return mockApiError;
}

function sessionHeaders(): Record<string, string> {
  const sessionId = getParentSessionId();
  const headers: Record<string, string> = { Accept: "application/json" };
  if (sessionId) {
    headers["X-Parent-Session-Id"] = sessionId;
    headers.Authorization = `Bearer ${sessionId}`;
  }
  return headers;
}

async function getJson<T>(path: string): Promise<{ ok: true; data: T } | { ok: false; error: ApiErrorEnvelope }> {
  try {
    const res = await fetch(`${apiBase()}${path}`, {
      credentials: "include",
      headers: sessionHeaders(),
    });
    if (!res.ok) return { ok: false, error: await parseError(res) };
    return { ok: true, data: (await res.json()) as T };
  } catch {
    return { ok: false, error: mockApiError };
  }
}

async function postJson<T>(
  path: string,
  body: unknown
): Promise<{ ok: true; data: T } | { ok: false; error: ApiErrorEnvelope }> {
  try {
    const res = await fetch(`${apiBase()}${path}`, {
      method: "POST",
      credentials: "include",
      headers: {
        ...sessionHeaders(),
        "Content-Type": "application/json",
      },
      body: JSON.stringify(body),
    });
    if (!res.ok) return { ok: false, error: await parseError(res) };
    return { ok: true, data: (await res.json()) as T };
  } catch {
    return { ok: false, error: mockApiError };
  }
}

export type OtpChallengeResult =
  | { ok: true; challengeId: string; expiresInSeconds: number; devCode?: string | null }
  | { ok: false; error: ApiErrorEnvelope };

export type OtpVerifyResult =
  | { ok: true; sessionToken: string; linkedStudentCount: number }
  | { ok: false; error: ApiErrorEnvelope };

export async function requestOtp(mobileLocal: string): Promise<OtpChallengeResult> {
  if (useMock()) {
    await delay(null, 400);
    const digits = digitsOnly(mobileLocal);
    if (!/^3\d{9}$/.test(digits)) {
      return {
        ok: false,
        error: {
          error: {
            code: "VALIDATION_FAILED",
            message: "One or more fields are invalid.",
            messageKey: "errors.validation_failed",
            correlationId: "f0000010-aaaa-bbbb-cccc-dddddddddddd",
            details: [
              {
                field: "mobileNumber",
                code: "INVALID_FORMAT",
                message: "Enter a Pakistani mobile number starting with 3.",
                messageKey: "errors.mobile_invalid",
              },
            ],
            retryable: false,
          },
        },
      };
    }
    if (digits === MOCK_RATE_LIMIT_MOBILE) return { ok: false, error: mockOtpRateLimited };
    return { ok: true, challengeId: `chal-${digits}`, expiresInSeconds: 300, devCode: MOCK_OTP_CODE };
  }

  const res = await postJson<{
    challengeId: string;
    expiresAtUtc: string;
    devCode?: string | null;
  }>("/api/v1/auth/otp/challenge", { mobile: mobileLocal });
  if (!res.ok) return res;
  const expires = new Date(res.data.expiresAtUtc).getTime();
  const expiresInSeconds = Math.max(1, Math.round((expires - Date.now()) / 1000));
  return {
    ok: true,
    challengeId: res.data.challengeId,
    expiresInSeconds,
    devCode: res.data.devCode,
  };
}

export async function verifyOtp(input: {
  challengeId: string;
  mobileLocal: string;
  code: string;
}): Promise<OtpVerifyResult> {
  if (useMock()) {
    await delay(null, 400);
    const digits = digitsOnly(input.mobileLocal);
    if (digits === MOCK_RATE_LIMIT_MOBILE) return { ok: false, error: mockOtpRateLimited };
    if (input.code !== MOCK_OTP_CODE) return { ok: false, error: mockOtpWrong };
    if (digits === MOCK_UNLINKED_MOBILE) return { ok: false, error: mockNoStudents };
    return {
      ok: true,
      sessionToken: `mock-session-${digits}`,
      linkedStudentCount: digits === MOCK_LINKED_MOBILE ? 1 : 1,
    };
  }

  const res = await postJson<{
    sessionId: string;
    accessToken: string;
    refreshToken: string;
    linkedStudentCount: number;
  }>("/api/v1/auth/otp/verify", {
    challengeId: input.challengeId,
    code: input.code,
  });
  if (!res.ok) return res;
  return {
    ok: true,
    sessionToken: res.data.accessToken || res.data.sessionId,
    linkedStudentCount: res.data.linkedStudentCount ?? 1,
  };
}

type ApiDashboard = {
  guardianDisplayName: string;
  mobileE164: string;
  studentId: string;
  studentName: string;
  studentExternalId: string;
  classLabel: string;
  tenantId: string;
  institutionName: string;
  tenantBrandHex: string;
  invoiceId?: string | null;
  feeAmount: string;
  dueDate: string;
  paymentStatus: string;
  canPay: boolean;
  isOverdue: boolean;
};

function mapDashboard(d: ApiDashboard): GuardianFeeDashboard {
  return {
    guardianDisplayName: d.guardianDisplayName,
    mobileE164: d.mobileE164,
    studentId: d.studentId,
    studentName: d.studentName,
    studentExternalId: d.studentExternalId,
    classLabel: d.classLabel,
    tenantId: d.tenantId,
    institutionName: d.institutionName,
    tenantBrandCss: d.tenantBrandHex,
    invoiceId: d.invoiceId ?? null,
    feeAmount: d.feeAmount,
    dueDate: d.dueDate,
    paymentStatus: d.paymentStatus as GuardianFeeDashboard["paymentStatus"],
    canPay: d.canPay,
    isOverdue: d.isOverdue,
  };
}

export async function fetchGuardianDashboard(): Promise<LoadState<GuardianFeeDashboard>> {
  if (useMock()) {
    await delay(null, 300);
    return {
      status: "success",
      data: {
        guardianDisplayName: "Guardian One",
        mobileE164: "+923001234567",
        studentId: "66666666-6666-6666-6666-666666660001",
        studentName: "Student One",
        studentExternalId: "STU-PILOT-0001",
        classLabel: "BS Computer Science",
        tenantId: "11111111-1111-1111-1111-111111111111",
        institutionName: "Pilot University",
        tenantBrandCss: "#0B3D91",
        invoiceId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbb0001",
        feeAmount: "71250.00",
        dueDate: "2026-10-14",
        paymentStatus: "Issued",
        canPay: true,
        isOverdue: false,
      },
    };
  }

  const res = await getJson<ApiDashboard>("/api/v1/parent/dashboard");
  if (!res.ok) {
    if (res.error.error.code === "NO_LINKED_STUDENTS") return { status: "empty" };
    return { status: "error", error: res.error };
  }
  return { status: "success", data: mapDashboard(res.data) };
}

export async function fetchParentPayContext(
  studentId: string
): Promise<LoadState<ParentPayContext>> {
  if (useMock()) {
    await delay(null, 300);
    const fee = (await fetchGuardianDashboard()) as Extract<
      LoadState<GuardianFeeDashboard>,
      { status: "success" }
    >;
    return {
      status: "success",
      data: {
        fee: fee.data,
        payableType: "Invoice",
        payableId: fee.data.invoiceId!,
        channels: mockParentChannels(),
      },
    };
  }

  const res = await getJson<{
    fee: ApiDashboard;
    payableType: string;
    payableId: string;
    channels: Array<{
      adapterCode: string;
      displayName: string;
      tier: number;
      meta: string;
      handoffLabel: string;
    }>;
  }>(`/api/v1/parent/students/${studentId}/pay-context`);
  if (!res.ok) return { status: "error", error: res.error };
  return {
    status: "success",
    data: {
      fee: mapDashboard(res.data.fee),
      payableType: res.data.payableType,
      payableId: res.data.payableId,
      channels: res.data.channels.map(
        (c): ParentPaymentChannel => ({
          adapterCode: c.adapterCode,
          displayName: c.displayName,
          tier: c.tier as 1 | 2 | 3,
          meta: c.meta,
          handoffLabel: c.handoffLabel,
        })
      ),
    },
  };
}

export async function createParentPayment(input: {
  studentId: string;
  adapterCode: string;
  idempotencyKey: string;
  invoiceId?: string | null;
}): Promise<
  | { ok: true; attemptId: string; status: string; checkoutUrl?: string | null }
  | { ok: false; error: ApiErrorEnvelope }
> {
  if (useMock()) {
    await delay(null, 500);
    return {
      ok: true,
      attemptId: `att-mock-${Date.now()}`,
      status: "Pending",
      checkoutUrl: null,
    };
  }

  const res = await postJson<{
    attemptId: string;
    status: string;
    checkoutUrl?: string | null;
  }>("/api/v1/parent/payments", {
    studentId: input.studentId,
    adapterCode: input.adapterCode,
    idempotencyKey: input.idempotencyKey,
    invoiceId: input.invoiceId ?? null,
  });
  if (!res.ok) return res;
  return {
    ok: true,
    attemptId: res.data.attemptId,
    status: res.data.status,
    checkoutUrl: res.data.checkoutUrl,
  };
}

function mockParentChannels(): ParentPaymentChannel[] {
  return [
    {
      adapterCode: "easypaisa-pilot",
      displayName: "Easypaisa",
      tier: 1,
      meta: "Pay with your Easypaisa account",
      handoffLabel: "Opening Easypaisa…",
    },
    {
      adapterCode: "jazzcash-pilot",
      displayName: "JazzCash",
      tier: 1,
      meta: "Pay with your JazzCash account",
      handoffLabel: "Opening JazzCash…",
    },
    {
      adapterCode: "card-psp-pilot",
      displayName: "Credit/Debit Card",
      tier: 2,
      meta: "Secure card payment on the bank page",
      handoffLabel: "Opening card payment…",
    },
  ];
}

export type ParentPaymentAttemptView = {
  id: string;
  status:
    | "Created"
    | "Pending"
    | "Succeeded"
    | "Failed"
    | "Expired"
    | "Cancelled"
    | "Reversed";
  amount: string;
  adapterCode: string;
  studentId: string;
  checkoutUrl: string | null;
  receiptId: string | null;
};

export async function fetchParentPaymentAttempt(
  attemptId: string
): Promise<LoadState<ParentPaymentAttemptView>> {
  if (useMock()) {
    await delay(null, 300);
    return {
      status: "success",
      data: {
        id: attemptId,
        status: "Succeeded",
        amount: "71250.00",
        adapterCode: "easypaisa-pilot",
        studentId: "66666666-6666-6666-6666-666666660001",
        checkoutUrl: null,
        receiptId: null,
      },
    };
  }

  const res = await getJson<{
    attemptId: string;
    status: string;
    amount: string;
    adapterCode: string;
    studentId: string;
    checkoutUrl?: string | null;
    receiptId?: string | null;
  }>(`/api/v1/parent/payments/attempts/${attemptId}`);
  if (!res.ok) return { status: "error", error: res.error };
  return {
    status: "success",
    data: {
      id: res.data.attemptId,
      status: res.data.status as ParentPaymentAttemptView["status"],
      amount: res.data.amount,
      adapterCode: res.data.adapterCode,
      studentId: res.data.studentId,
      checkoutUrl: res.data.checkoutUrl ?? null,
      receiptId: res.data.receiptId ?? null,
    },
  };
}

export type ParentPaymentHistoryItem = {
  attemptId: string;
  receiptId: string | null;
  amount: string;
  status: ParentPaymentAttemptView["status"];
  adapterCode: string;
  channelName: string;
  paidAtUtc: string;
  studentName: string;
  termLabel: string;
};

export async function fetchParentPaymentHistory(
  studentId: string
): Promise<LoadState<ParentPaymentHistoryItem[]>> {
  if (useMock()) {
    await delay(null, 300);
    return { status: "empty" };
  }

  const res = await getJson<
    Array<{
      attemptId: string;
      receiptId?: string | null;
      amount: string;
      status: string;
      adapterCode: string;
      channelName: string;
      paidAtUtc: string;
      studentName: string;
      termLabel: string;
    }>
  >(`/api/v1/parent/students/${encodeURIComponent(studentId)}/payments/history`);

  if (!res.ok) return { status: "error", error: res.error };
  if (!res.data.length) return { status: "empty" };

  return {
    status: "success",
    data: res.data.map((item) => ({
      attemptId: item.attemptId,
      receiptId: item.receiptId ?? null,
      amount: item.amount,
      status: item.status as ParentPaymentAttemptView["status"],
      adapterCode: item.adapterCode,
      channelName: item.channelName,
      paidAtUtc: item.paidAtUtc,
      studentName: item.studentName,
      termLabel: item.termLabel,
    })),
  };
}

