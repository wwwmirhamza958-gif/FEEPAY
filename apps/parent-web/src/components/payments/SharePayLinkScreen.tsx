"use client";

import { Amount, Button, ConfirmDialog } from "@fee/ui";
import { formatPkr } from "@fee/money";
import { Check, Copy, Share2 } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { MockBanner, StickyActionBar } from "@/components/PhoneShell";
import {
  ScreenBody,
  ScreenEmpty,
  ScreenError,
  ScreenLoading,
} from "@/components/ScreenStates";
import { formatDueDate } from "@/lib/format";
import {
  createPayLink,
  fetchPayContext,
  revokePayLink,
  type PayLink,
} from "@/lib/payments";
import type { LoadState } from "@/lib/types";

export function SharePayLinkScreen({ studentId }: { studentId: string }) {
  const router = useRouter();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<LoadState<never> | null>(null);
  const [link, setLink] = useState<PayLink | null>(null);
  const [copied, setCopied] = useState(false);
  const [revokeOpen, setRevokeOpen] = useState(false);
  const [revoking, setRevoking] = useState(false);
  const [empty, setEmpty] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    const ctx = await fetchPayContext(studentId);
    if (ctx.status === "error") {
      setError({ status: "error", error: ctx.error });
      setLoading(false);
      return;
    }
    if (ctx.status === "empty" || ctx.status === "loading") {
      setEmpty(true);
      setLoading(false);
      return;
    }
    const amount =
      ctx.data.instalmentAmount !== "0.00"
        ? ctx.data.instalmentAmount
        : ctx.data.fullBalanceAmount;
    const res = await createPayLink({ studentId, amount });
    if (!res.ok) {
      setError({ status: "error", error: res.error });
      setLoading(false);
      return;
    }
    setLink(res.data);
    setLoading(false);
  }, [studentId]);

  useEffect(() => {
    void load();
  }, [load]);

  const shareWhatsApp = async () => {
    if (!link) return;
    const text = `Please pay ${formatPkr(link.preview.amount)} for ${link.preview.studentInitials} (${link.preview.termLabel}) at ${link.preview.institutionName}. ${link.url}`;
    if (navigator.share) {
      try {
        await navigator.share({ title: "Pay on behalf", text, url: link.url });
        return;
      } catch {
        /* fall through */
      }
    }
    window.open(
      `https://wa.me/?text=${encodeURIComponent(text)}`,
      "_blank",
      "noopener,noreferrer"
    );
  };

  const copyLink = async () => {
    if (!link) return;
    try {
      await navigator.clipboard.writeText(link.url);
      setCopied(true);
      window.setTimeout(() => setCopied(false), 1500);
    } catch {
      /* ignore */
    }
  };

  if (loading) return <ScreenLoading rows={4} />;
  if (error?.status === "error") {
    return <ScreenError error={error.error} onRetry={() => void load()} />;
  }
  if (empty || !link) {
    return (
      <ScreenEmpty
        title="Nothing to share — this statement is already paid."
        actionLabel="Back to statement"
        onAction={() => router.push(`/students/${studentId}/statement`)}
      />
    );
  }

  const preview = link.preview;

  return (
    <ScreenBody
      footer={
        <StickyActionBar>
          <Button type="button" className="w-full" onClick={() => void shareWhatsApp()}>
            <Share2 size={18} aria-hidden="true" />
            Share on WhatsApp
          </Button>
          <Button
            type="button"
            variant="secondary"
            className="mt-[var(--space-8)] w-full"
            onClick={() => void copyLink()}
          >
            {copied ? (
              <>
                <Check size={18} aria-hidden="true" />
                Copied
              </>
            ) : (
              <>
                <Copy size={18} aria-hidden="true" />
                Copy link
              </>
            )}
          </Button>
        </StickyActionBar>
      }
    >
      <MockBanner />
      <div className="px-[var(--space-16)] pb-[var(--space-24)] pt-[var(--space-16)]">
        <Link
          href={`/pay/${studentId}`}
          className="inline-flex min-h-11 items-center font-[var(--font-body-strong)] text-accent touch-manipulation"
        >
          Back to pay
        </Link>
        <h1 className="mt-[var(--space-12)] font-[var(--font-title-1)] text-ink">
          Share pay-on-behalf link
        </h1>
        <p className="mt-[var(--space-8)] font-[var(--font-body)] text-ink-secondary">
          Preview of exactly what the recipient will see.
        </p>

        <div
          className="fee-tenant-band mt-[var(--space-20)] rounded-fee-16 bg-surface p-[var(--space-20)] shadow-fee-1"
          style={{ ["--tenant-brand" as string]: preview.tenantBrandCss }}
        >
          <p className="font-[var(--font-label)] text-ink-secondary">
            {preview.institutionName}
          </p>
          <p className="mt-[var(--space-12)] font-[var(--font-title-1)] text-ink">
            {preview.studentInitials}
          </p>
          <p className="mt-[var(--space-4)] font-[var(--font-body)] text-ink-secondary">
            {preview.termLabel}
          </p>
          <p className="mt-[var(--space-16)] fee-display-amount tabular-nums text-ink">
            <Amount>{formatPkr(preview.amount)}</Amount>
          </p>
          <p className="mt-[var(--space-8)] font-[var(--font-body)] text-ink-secondary">
            Due <bdi>{formatDueDate(preview.dueDate)}</bdi>
          </p>
          <p className="mt-[var(--space-12)] font-[var(--font-label)] text-ink-tertiary">
            {preview.requestedByLabel}
          </p>
        </div>

        <p className="mt-[var(--space-16)] font-[var(--font-label)] text-ink-secondary">
          Link works until{" "}
          <bdi>{formatDueDate(link.expiresAt.slice(0, 10))}</bdi>
        </p>

        {!link.revoked ? (
          <button
            type="button"
            className="mt-[var(--space-24)] inline-flex min-h-11 font-[var(--font-body-strong)] text-danger touch-manipulation"
            onClick={() => setRevokeOpen(true)}
          >
            Revoke link
          </button>
        ) : (
          <p className="mt-[var(--space-24)] font-[var(--font-body)] text-ink-secondary">
            This link has been revoked.
          </p>
        )}
      </div>

      <ConfirmDialog
        open={revokeOpen}
        title="Revoke this link?"
        description="Anyone with the link will no longer be able to pay. This cannot be undone."
        confirmLabel="Revoke link"
        cancelLabel="Keep link"
        destructive
        dense={false}
        busy={revoking}
        onCancel={() => setRevokeOpen(false)}
        onConfirm={() => {
          void (async () => {
            setRevoking(true);
            const res = await revokePayLink(preview.token);
            setRevoking(false);
            setRevokeOpen(false);
            if (res.ok) {
              setLink({ ...link, revoked: true });
            }
          })();
        }}
      />
    </ScreenBody>
  );
}
