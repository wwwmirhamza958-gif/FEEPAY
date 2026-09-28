/**
 * Mock parent data client.
 * TODO(Agent C): replace with @fee/api-client once parent OpenAPI paths land.
 * See docs/supervisor/REQUESTS_TO_C_FROM_F.md
 */

import type { ApiErrorEnvelope } from "@fee/api-client";
import {
  delay,
  MOCK_LINKED_MOBILE,
  MOCK_OTP_CODE,
  MOCK_RATE_LIMIT_MOBILE,
  MOCK_UNLINKED_MOBILE,
  mockAllPaidHome,
  mockApiError,
  mockFamilyHome,
  mockNoStudents,
  mockOtpRateLimited,
  mockOtpWrong,
  mockStatements,
} from "./data";
import type { FamilyHome, LoadState, StudentStatement } from "../types";

export type MockMode = "success" | "empty" | "error" | "allPaid";

let homeMode: MockMode = "success";
let statementMode: MockMode = "success";

export function setMockHomeMode(mode: MockMode) {
  homeMode = mode;
}

export function setMockStatementMode(mode: MockMode) {
  statementMode = mode;
}

export function getMockHomeMode(): MockMode {
  return homeMode;
}

export type OtpChallengeResult =
  | { ok: true; challengeId: string; expiresInSeconds: number }
  | { ok: false; error: ApiErrorEnvelope };

export type OtpVerifyResult =
  | { ok: true; sessionToken: string; linkedStudentCount: number }
  | { ok: false; error: ApiErrorEnvelope };

/** Normalise local 10-digit PK mobile (3XXXXXXXXX). */
export function digitsOnly(value: string): string {
  return value.replace(/\D/g, "");
}

export async function requestOtp(mobileLocal: string): Promise<OtpChallengeResult> {
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
  if (digits === MOCK_RATE_LIMIT_MOBILE) {
    return { ok: false, error: mockOtpRateLimited };
  }
  // TODO: POST /api/v1/auth/otp/challenge
  return { ok: true, challengeId: `chal-${digits}`, expiresInSeconds: 300 };
}

export async function verifyOtp(input: {
  challengeId: string;
  mobileLocal: string;
  code: string;
}): Promise<OtpVerifyResult> {
  await delay(null, 400);
  const digits = digitsOnly(input.mobileLocal);

  if (digits === MOCK_RATE_LIMIT_MOBILE) {
    return { ok: false, error: mockOtpRateLimited };
  }
  if (input.code !== MOCK_OTP_CODE) {
    return { ok: false, error: mockOtpWrong };
  }
  if (digits === MOCK_UNLINKED_MOBILE) {
    return { ok: false, error: mockNoStudents };
  }
  // TODO: POST /api/v1/auth/otp/verify
  const linked =
    digits === MOCK_LINKED_MOBILE
      ? mockFamilyHome.groups.reduce((n, g) => n + g.students.length, 0)
      : 1;
  return {
    ok: true,
    sessionToken: `mock-session-${digits}`,
    linkedStudentCount: linked,
  };
}

export async function fetchFamilyHome(): Promise<LoadState<FamilyHome>> {
  await delay(null);
  if (homeMode === "error") return { status: "error", error: mockApiError };
  if (homeMode === "empty") return { status: "empty" };
  if (homeMode === "allPaid") return { status: "success", data: mockAllPaidHome };
  return { status: "success", data: mockFamilyHome };
}

export async function fetchStudentStatement(
  studentId: string
): Promise<LoadState<StudentStatement>> {
  await delay(null);
  if (statementMode === "error") return { status: "error", error: mockApiError };
  if (statementMode === "empty") return { status: "empty" };
  const statement = mockStatements[studentId];
  if (!statement) {
    return {
      status: "error",
      error: {
        error: {
          code: "RESOURCE_NOT_FOUND",
          message: "We couldn't find that statement.",
          messageKey: "errors.resource_not_found",
          correlationId: "f0000020-aaaa-bbbb-cccc-dddddddddddd",
          details: [],
          retryable: false,
        },
      },
    };
  }
  return { status: "success", data: statement };
}
