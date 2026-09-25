"use client";

import { Button, EmptyState, ErrorState } from "@fee/ui";
import { useCallback, useEffect, useState, type CSSProperties } from "react";
import {
  FieldLabel,
  MockBanner,
  PageHeader,
  Panel,
  TableSkeleton,
  TextInput,
} from "@/components/ui-bits";
import { fetchBranding, saveBranding } from "@/lib/mock/client";
import type { BrandingConfig, LoadState } from "@/lib/types";

export default function BrandingPage() {
  const [state, setState] = useState<LoadState<BrandingConfig>>({
    status: "loading",
  });
  const [name, setName] = useState("");
  const [saving, setSaving] = useState(false);
  const [savedMsg, setSavedMsg] = useState<string | null>(null);

  const load = useCallback(() => {
    setState({ status: "loading" });
    void fetchBranding().then((res) => {
      setState(res);
      if (res.status === "success") setName(res.data.tenantName);
    });
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  async function onSave() {
    setSaving(true);
    setSavedMsg(null);
    const res = await saveBranding({ tenantName: name });
    setSaving(false);
    if (!res.ok) {
      setState({ status: "error", error: res.error });
      return;
    }
    setSavedMsg("Saved locally (mock). TODO: POST branding to C.");
  }

  return (
    <>
      <PageHeader
        title="Branding"
        description="Tenant colour is for identity only — logo chip and a 3 px band. Never on buttons."
        primaryAction={
          state.status === "success" ? (
            <Button size="portal" onClick={() => void onSave()} loading={saving}>
              Save branding
            </Button>
          ) : null
        }
      />
      <MockBanner />

      {state.status === "loading" ? <TableSkeleton rows={3} cols={2} /> : null}

      {state.status === "empty" ? (
        <EmptyState title="No branding configured yet." />
      ) : null}

      {state.status === "error" ? (
        <ErrorState message={state.error.error.message} onRetry={load} />
      ) : null}

      {state.status === "success" ? (
        <Panel className="max-w-xl">
          <div
            className="fee-tenant-band mb-[var(--space-16)] rounded-fee-12 border border-border bg-surface p-[var(--space-16)]"
            style={
              {
                ["--tenant-brand"]: "var(--accent)",
              } as CSSProperties
            }
          >
            <div className="flex items-center gap-[var(--space-12)]">
              <div className="fee-tenant-chip flex items-center justify-center text-[12px] font-semibold">
                ID
              </div>
              <div>
                <p className="font-semibold text-ink">{name || state.data.tenantName}</p>
                <p className="text-[14px] text-ink-secondary">Identity preview</p>
              </div>
            </div>
          </div>

          <FieldLabel htmlFor="tenant-name">Institution display name</FieldLabel>
          <TextInput
            id="tenant-name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            autoComplete="organization"
          />

          <p className="mt-[var(--space-16)] text-[14px] text-ink-secondary">
            {state.data.brandColourTokenNote}
          </p>
          <p className="mt-[var(--space-8)] text-[14px] text-ink-tertiary">
            Brand colour CSS: <code>{state.data.brandColourCssVar}</code> (set via
            tenant config API — not a button fill).
          </p>

          {savedMsg ? (
            <p className="mt-[var(--space-12)] text-[14px] text-success" role="status">
              {savedMsg}
            </p>
          ) : null}
        </Panel>
      ) : null}
    </>
  );
}
