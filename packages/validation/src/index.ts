import { z } from "zod";

/**
 * Shared validation — CONTRACT.md §2, §3, §6.2
 */

export const apiErrorDetailSchema = z.object({
  field: z.string().nullable(),
  code: z.string().min(1),
  message: z.string().min(1),
  messageKey: z.string().min(1),
});

export const apiErrorSchema = z.object({
  code: z.string().min(1),
  message: z.string().min(1),
  messageKey: z.string().min(1),
  correlationId: z.string().uuid(),
  details: z.array(apiErrorDetailSchema),
  retryable: z.boolean(),
});

export const apiErrorEnvelopeSchema = z.object({
  error: apiErrorSchema,
});

export type ApiErrorDetail = z.infer<typeof apiErrorDetailSchema>;
export type ApiError = z.infer<typeof apiErrorSchema>;
export type ApiErrorEnvelope = z.infer<typeof apiErrorEnvelopeSchema>;

const FALLBACK_ERROR: ApiErrorEnvelope = {
  error: {
    code: "UNKNOWN_ERROR",
    message: "Something went wrong. Please try again.",
    messageKey: "errors.unknown",
    correlationId: "00000000-0000-0000-0000-000000000000",
    details: [],
    retryable: true,
  },
};

/**
 * Always returns a usable §3 envelope — never silent/empty (CONTRACT §3, §6.2).
 */
export function parseApiError(body: unknown): ApiErrorEnvelope {
  const parsed = apiErrorEnvelopeSchema.safeParse(body);
  if (parsed.success) return parsed.data;

  // Tolerate a bare error object without wrapper.
  const bare = apiErrorSchema.safeParse(
    body && typeof body === "object" && "error" in (body as object)
      ? (body as { error: unknown }).error
      : body
  );
  if (bare.success) return { error: bare.data };

  if (body && typeof body === "object" && "message" in (body as object)) {
    const msg = String((body as { message: unknown }).message || "").trim();
    if (msg) {
      return {
        error: {
          ...FALLBACK_ERROR.error,
          message: msg,
          code: "UNTYPED_ERROR",
          messageKey: "errors.untyped",
        },
      };
    }
  }

  return FALLBACK_ERROR;
}

export const mobilePk = z
  .string()
  .trim()
  .transform((v) => v.replace(/[\s-]/g, ""))
  .refine((v) => /^(\+92|0)?3\d{9}$/.test(v), {
    message: "Enter a Pakistani mobile number starting with 3.",
  })
  .transform((v) => {
    const digits = v.replace(/\D/g, "");
    if (digits.startsWith("92") && digits.length === 12) return `+${digits}`;
    if (digits.startsWith("0") && digits.length === 11) return `+92${digits.slice(1)}`;
    if (digits.length === 10) return `+92${digits}`;
    return v.startsWith("+") ? v : `+${digits}`;
  });

export const otpCode = z.string().regex(/^\d{6}$/, "Enter the 6-digit code");

export const decimalAmount = z
  .string()
  .regex(/^-?\d+\.\d{2}$/, "Amount must be a decimal string with two fractional digits");

export const uuid = z.string().uuid();

export const pageParams = z.object({
  page: z.coerce.number().int().min(1).default(1),
  pageSize: z.coerce.number().int().min(1).max(100).default(20),
});

export const portalLoginSchema = z.object({
  email: z.string().email("Enter a valid email"),
  password: z.string().min(8, "Password must be at least 8 characters"),
  totpCode: z.string().regex(/^\d{6}$/).optional(),
});

export const otpChallengeRequestSchema = z.object({
  mobile: mobilePk,
});

export const otpVerifySchema = z.object({
  challengeId: uuid,
  code: otpCode,
});

/** CONTRACT §2 — do not invent synonyms. */
export const PaymentAttemptStatus = {
  Created: "Created",
  Pending: "Pending",
  Succeeded: "Succeeded",
  Failed: "Failed",
  Expired: "Expired",
  Cancelled: "Cancelled",
  Reversed: "Reversed",
} as const;

export type PaymentAttemptStatus =
  (typeof PaymentAttemptStatus)[keyof typeof PaymentAttemptStatus];

export const InvoiceStatus = {
  Draft: "Draft",
  Issued: "Issued",
  PartiallyPaid: "PartiallyPaid",
  Paid: "Paid",
  Superseded: "Superseded",
} as const;

export type InvoiceStatus = (typeof InvoiceStatus)[keyof typeof InvoiceStatus];

export const ClearanceStatus = {
  Cleared: "Cleared",
  ClearedWithDeferment: "ClearedWithDeferment",
  NotCleared: "NotCleared",
} as const;

export type ClearanceStatus = (typeof ClearanceStatus)[keyof typeof ClearanceStatus];

/** Derived display statuses — not stored as Invoice.status (CONTRACT §2.2). */
export const DerivedDisplayStatus = {
  Overdue: "Overdue",
  DueSoon: "DueSoon",
} as const;
