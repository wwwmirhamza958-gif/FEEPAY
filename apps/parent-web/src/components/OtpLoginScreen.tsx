"use client";

import { Button } from "@fee/ui";
import { useRouter } from "next/navigation";
import {
  useEffect,
  useId,
  useRef,
  useState,
  type ClipboardEvent,
  type FormEvent,
  type KeyboardEvent,
} from "react";
import { MockBanner, PhoneShell, StickyActionBar } from "@/components/PhoneShell";
import { formatCountdown, formatPkMobileInput } from "@/lib/format";
import { digitsOnly, requestOtp, verifyOtp } from "@/lib/mock";
import { MOCK_LINKED_MOBILE, MOCK_OTP_CODE } from "@/lib/mock/data";

type Step = "mobile" | "otp";

export function OtpLoginScreen() {
  const router = useRouter();
  const mobileId = useId();
  const otpHintId = useId();
  const [step, setStep] = useState<Step>("mobile");
  const [mobile, setMobile] = useState(formatPkMobileInput(MOCK_LINKED_MOBILE));
  const [challengeId, setChallengeId] = useState<string | null>(null);
  const [digits, setDigits] = useState<string[]>(["", "", "", "", "", ""]);
  const [fieldError, setFieldError] = useState<string | null>(null);
  const [bannerError, setBannerError] = useState<string | null>(null);
  const [rateLimitSeconds, setRateLimitSeconds] = useState(0);
  const [resendSeconds, setResendSeconds] = useState(0);
  const [sending, setSending] = useState(false);
  const [verifying, setVerifying] = useState(false);
  const [shake, setShake] = useState(false);
  const inputsRef = useRef<Array<HTMLInputElement | null>>([]);

  useEffect(() => {
    if (rateLimitSeconds <= 0) return;
    const t = setInterval(() => {
      setRateLimitSeconds((s) => Math.max(0, s - 1));
    }, 1000);
    return () => clearInterval(t);
  }, [rateLimitSeconds]);

  useEffect(() => {
    if (resendSeconds <= 0) return;
    const t = setInterval(() => {
      setResendSeconds((s) => Math.max(0, s - 1));
    }, 1000);
    return () => clearInterval(t);
  }, [resendSeconds]);

  async function onSendCode(e?: FormEvent) {
    e?.preventDefault();
    if (rateLimitSeconds > 0 || sending) return;
    setFieldError(null);
    setBannerError(null);
    setSending(true);
    const res = await requestOtp(digitsOnly(mobile));
    setSending(false);
    if (!res.ok) {
      if (res.error.error.code === "OTP_RATE_LIMITED") {
        setRateLimitSeconds(600);
        setBannerError(res.error.error.message);
        return;
      }
      const detail = res.error.error.details[0]?.message;
      setFieldError(detail ?? res.error.error.message);
      return;
    }
    setChallengeId(res.challengeId);
    setDigits(["", "", "", "", "", ""]);
    setStep("otp");
    setResendSeconds(45);
    queueMicrotask(() => inputsRef.current[0]?.focus());
  }

  async function submitCode(code: string) {
    if (!challengeId || verifying || rateLimitSeconds > 0) return;
    setBannerError(null);
    setVerifying(true);
    const res = await verifyOtp({
      challengeId,
      mobileLocal: digitsOnly(mobile),
      code,
    });
    setVerifying(false);
    if (!res.ok) {
      if (res.error.error.code === "OTP_RATE_LIMITED") {
        setRateLimitSeconds(600);
        setBannerError(res.error.error.message);
        return;
      }
      if (res.error.error.code === "NO_LINKED_STUDENTS") {
        setBannerError(res.error.error.message);
        return;
      }
      setBannerError(res.error.error.message);
      setShake(true);
      window.setTimeout(() => setShake(false), 320);
      setDigits(["", "", "", "", "", ""]);
      queueMicrotask(() => inputsRef.current[0]?.focus());
      return;
    }
    if (typeof window !== "undefined") {
      window.sessionStorage.setItem("fee.parent.session", res.sessionToken);
    }
    router.push("/home");
  }

  function updateDigit(index: number, value: string) {
    const cleaned = value.replace(/\D/g, "");
    if (cleaned.length > 1) {
      applyOtpPaste(cleaned);
      return;
    }
    const next = [...digits];
    next[index] = cleaned.slice(-1);
    setDigits(next);
    if (cleaned && index < 5) {
      inputsRef.current[index + 1]?.focus();
    }
    const joined = next.join("");
    if (joined.length === 6) {
      void submitCode(joined);
    }
  }

  function applyOtpPaste(raw: string) {
    const chars = raw.replace(/\D/g, "").slice(0, 6).split("");
    const next = ["", "", "", "", "", ""];
    chars.forEach((c, i) => {
      next[i] = c;
    });
    setDigits(next);
    const focusAt = Math.min(chars.length, 5);
    inputsRef.current[focusAt]?.focus();
    if (chars.length === 6) {
      void submitCode(chars.join(""));
    }
  }

  function onOtpKeyDown(index: number, e: KeyboardEvent<HTMLInputElement>) {
    if (e.key === "Backspace" && !digits[index] && index > 0) {
      inputsRef.current[index - 1]?.focus();
    }
  }

  function onOtpPaste(e: ClipboardEvent<HTMLInputElement>) {
    e.preventDefault();
    applyOtpPaste(e.clipboardData.getData("text"));
  }

  async function onResend() {
    if (resendSeconds > 0 || rateLimitSeconds > 0) return;
    await onSendCode();
  }

  const rateLimited = rateLimitSeconds > 0;

  return (
    <PhoneShell>
      <MockBanner />
      <div className="flex flex-1 flex-col px-[var(--space-16)] pt-[var(--space-32)]">
        <div className="mb-[var(--space-24)] flex items-center gap-[var(--space-12)]">
          <span
            className="fee-tenant-chip flex items-center justify-center font-[var(--font-label)] text-ink"
            aria-hidden="true"
          >
            Fee
          </span>
          <p className="font-[var(--font-label)] text-ink-secondary">Fee Platform</p>
        </div>

        {step === "mobile" ? (
          <form onSubmit={onSendCode} className="flex flex-1 flex-col">
            <h1 className="font-[var(--font-title-1)] text-ink">
              Enter your mobile number
            </h1>
            <p className="mt-[var(--space-8)] font-[var(--font-body)] text-ink-secondary">
              We&apos;ll text a one-time code. No password needed.
            </p>

            <label
              htmlFor={mobileId}
              className="mt-[var(--space-24)] font-[var(--font-label)] text-ink"
            >
              Mobile number
            </label>
            <div className="mt-[var(--space-8)] flex min-h-12 items-stretch overflow-hidden rounded-fee-12 border border-border-strong bg-surface">
              <span className="flex items-center border-e border-border bg-surface-sunken px-[var(--space-12)] font-[var(--font-body-strong)] tabular-nums text-ink">
                +92
              </span>
              <input
                id={mobileId}
                type="tel"
                inputMode="numeric"
                autoComplete="tel-national"
                placeholder="3XX XXXXXXX"
                value={mobile}
                onChange={(e) => {
                  setMobile(formatPkMobileInput(e.target.value));
                  setFieldError(null);
                }}
                aria-invalid={fieldError ? true : undefined}
                aria-describedby={fieldError ? `${mobileId}-err` : undefined}
                className="min-h-12 w-full bg-transparent px-[var(--space-12)] font-[var(--font-body)] text-[16px] text-ink outline-none"
              />
            </div>
            {fieldError ? (
              <p
                id={`${mobileId}-err`}
                role="alert"
                className="mt-[var(--space-8)] font-[var(--font-label)] text-danger"
              >
                {fieldError}
              </p>
            ) : null}

            {bannerError ? (
              <p
                role="alert"
                className="mt-[var(--space-16)] rounded-fee-12 bg-danger-soft px-[var(--space-12)] py-[var(--space-12)] font-[var(--font-body)] text-danger"
              >
                {bannerError}
                {rateLimited
                  ? ` (${formatCountdown(rateLimitSeconds)})`
                  : null}
              </p>
            ) : null}

            <p className="mt-[var(--space-16)] font-[var(--font-micro)] text-ink-tertiary">
              Demo: {formatPkMobileInput(MOCK_LINKED_MOBILE)} · code {MOCK_OTP_CODE}
            </p>

            <div className="flex-1" />
          </form>
        ) : (
          <div className="flex flex-1 flex-col">
            <h1 className="font-[var(--font-title-1)] text-ink">Enter the code</h1>
            <p className="mt-[var(--space-8)] font-[var(--font-body)] text-ink-secondary">
              Sent to{" "}
              <bdi className="tabular-nums">+92 {mobile}</bdi>
            </p>
            <button
              type="button"
              className="mt-[var(--space-8)] min-h-11 self-start font-[var(--font-body-strong)] text-accent touch-manipulation"
              onClick={() => {
                setStep("mobile");
                setBannerError(null);
                setDigits(["", "", "", "", "", ""]);
              }}
            >
              Wrong number? Edit
            </button>

            <div
              className={`mt-[var(--space-24)] flex justify-between gap-[var(--space-8)] ${
                shake ? "fee-otp-shake" : ""
              }`}
              role="group"
              aria-label="One-time code"
              aria-describedby={otpHintId}
            >
              {digits.map((d, i) => (
                <input
                  key={i}
                  ref={(el) => {
                    inputsRef.current[i] = el;
                  }}
                  type="text"
                  inputMode="numeric"
                  autoComplete={i === 0 ? "one-time-code" : "off"}
                  maxLength={1}
                  value={d}
                  aria-label={`Digit ${i + 1}`}
                  onChange={(e) => updateDigit(i, e.target.value)}
                  onKeyDown={(e) => onOtpKeyDown(i, e)}
                  onPaste={onOtpPaste}
                  className="min-h-12 w-full max-w-12 rounded-fee-12 border border-border-strong bg-surface text-center font-[var(--font-title-2)] tabular-nums text-ink outline-none focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
                />
              ))}
            </div>
            <p id={otpHintId} className="sr-only">
              Enter the six-digit code from your SMS. Pasting is supported.
            </p>

            {bannerError ? (
              <p
                role="alert"
                className="mt-[var(--space-16)] rounded-fee-12 bg-danger-soft px-[var(--space-12)] py-[var(--space-12)] font-[var(--font-body)] text-danger"
              >
                {bannerError}
                {rateLimited
                  ? ` (${formatCountdown(rateLimitSeconds)})`
                  : null}
              </p>
            ) : null}

            <button
              type="button"
              disabled={resendSeconds > 0 || rateLimited || sending}
              onClick={() => void onResend()}
              className="mt-[var(--space-16)] min-h-11 self-start font-[var(--font-body-strong)] text-accent touch-manipulation disabled:text-ink-tertiary"
            >
              {resendSeconds > 0
                ? `Resend in ${formatCountdown(resendSeconds)}`
                : "Resend code"}
            </button>

            <div className="flex-1" />
          </div>
        )}
      </div>

      {step === "mobile" ? (
        <StickyActionBar>
          <Button
            type="button"
            className="w-full"
            loading={sending}
            disabled={rateLimited || digitsOnly(mobile).length < 10}
            onClick={() => void onSendCode()}
          >
            Send code
          </Button>
        </StickyActionBar>
      ) : (
        <StickyActionBar>
          <Button
            type="button"
            className="w-full"
            loading={verifying}
            disabled={rateLimited || digits.join("").length < 6}
            onClick={() => void submitCode(digits.join(""))}
          >
            Continue
          </Button>
        </StickyActionBar>
      )}
    </PhoneShell>
  );
}
