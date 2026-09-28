"use client";

import { Button, EmptyState, ErrorState } from "@fee/ui";
import { useCallback, useEffect, useState } from "react";
import {
  FieldLabel,
  MockBanner,
  PageHeader,
  Panel,
  TableShell,
  TableSkeleton,
  Td,
  TextInput,
  Th,
} from "@/components/ui-bits";
import { createFeeHead, fetchFeeHeads } from "@/lib/api/client";
import type { FeeHead, LoadState } from "@/lib/types";

export default function FeeHeadsPage() {
  const [state, setState] = useState<LoadState<FeeHead[]>>({ status: "loading" });
  const [code, setCode] = useState("");
  const [name, setName] = useState("");
  const [saving, setSaving] = useState(false);
  const [writeError, setWriteError] = useState<string | null>(null);

  const load = useCallback(() => {
    setState({ status: "loading" });
    setWriteError(null);
    void fetchFeeHeads().then(setState);
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  async function onCreate() {
    if (!code.trim() || !name.trim()) return;
    setSaving(true);
    setWriteError(null);
    const res = await createFeeHead({ code: code.trim(), name: name.trim() });
    setSaving(false);
    if (!res.ok) {
      // 501 from FeeEngine ownership — surface §3 error.message
      setWriteError(res.error.error.message);
      return;
    }
    setCode("");
    setName("");
    load();
  }

  return (
    <>
      <PageHeader
        title="Fee heads"
        description="Named charge categories. Tax configuration UI is out of scope (CONTRACT §8.2)."
        primaryAction={
          <Button
            size="portal"
            onClick={() => void onCreate()}
            loading={saving}
            disabled={!code.trim() || !name.trim()}
          >
            Add fee head
          </Button>
        }
      />
      <MockBanner />

      <Panel className="mb-[var(--space-24)] max-w-2xl">
        <div className="grid gap-[var(--space-12)] sm:grid-cols-2">
          <div>
            <FieldLabel htmlFor="fh-code">Code</FieldLabel>
            <TextInput id="fh-code" value={code} onChange={(e) => setCode(e.target.value)} />
          </div>
          <div>
            <FieldLabel htmlFor="fh-name">Display name</FieldLabel>
            <TextInput id="fh-name" value={name} onChange={(e) => setName(e.target.value)} />
          </div>
        </div>
        <p className="mt-[var(--space-8)] text-[14px] text-ink-tertiary">
          Catalog is read-only from FeeEngine in P1; create may return 501.
        </p>
        {writeError ? (
          <p className="mt-[var(--space-12)] text-[14px] text-danger" role="alert">
            {writeError}
          </p>
        ) : null}
      </Panel>

      {state.status === "loading" ? <TableSkeleton /> : null}
      {state.status === "empty" ? (
        <EmptyState title="No fee heads yet. Add the first head above." />
      ) : null}
      {state.status === "error" ? (
        <ErrorState message={state.error.error.message} onRetry={load} />
      ) : null}
      {state.status === "success" ? (
        <TableShell caption="Fee heads">
          <thead>
            <tr>
              <Th>Code</Th>
              <Th>Name</Th>
              <Th>Tax prep flag</Th>
            </tr>
          </thead>
          <tbody>
            {state.data.map((h) => (
              <tr key={h.id}>
                <Td>{h.code}</Td>
                <Td>{h.name}</Td>
                <Td>{h.taxableFor236I ? "Flagged (unused)" : "—"}</Td>
              </tr>
            ))}
          </tbody>
        </TableShell>
      ) : null}
    </>
  );
}
