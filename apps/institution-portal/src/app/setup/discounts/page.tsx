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
import {
  createDiscountCategory,
  fetchDiscountCategories,
  type DiscountCategory,
} from "@/lib/api/client";
import type { LoadState } from "@/lib/types";

/**
 * Discount categories — GET is an empty stub until B persists; POST may 501.
 */
export default function DiscountsPage() {
  const [state, setState] = useState<LoadState<DiscountCategory[]>>({
    status: "loading",
  });
  const [name, setName] = useState("");
  const [saving, setSaving] = useState(false);
  const [writeError, setWriteError] = useState<string | null>(null);

  const load = useCallback(() => {
    setState({ status: "loading" });
    setWriteError(null);
    void fetchDiscountCategories().then(setState);
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  async function onCreate() {
    if (!name.trim()) return;
    setSaving(true);
    setWriteError(null);
    const res = await createDiscountCategory({ name: name.trim() });
    setSaving(false);
    if (!res.ok) {
      setWriteError(res.error.error.message);
      return;
    }
    setName("");
    load();
  }

  return (
    <>
      <PageHeader
        title="Discount categories"
        description="Categories and stacking policy. Writes are owned by FeeEngine until Billing persists them."
        primaryAction={
          <Button
            size="portal"
            onClick={() => void onCreate()}
            loading={saving}
            disabled={!name.trim()}
          >
            Add category
          </Button>
        }
      />
      <MockBanner />

      <Panel className="mb-[var(--space-24)] max-w-xl">
        <FieldLabel htmlFor="disc-name">Category name</FieldLabel>
        <TextInput
          id="disc-name"
          value={name}
          onChange={(e) => setName(e.target.value)}
        />
        {writeError ? (
          <p className="mt-[var(--space-12)] text-[14px] text-danger" role="alert">
            {writeError}
          </p>
        ) : null}
      </Panel>

      {state.status === "loading" ? <TableSkeleton rows={3} cols={2} /> : null}
      {state.status === "empty" ? (
        <EmptyState title="No discount categories yet." />
      ) : null}
      {state.status === "error" ? (
        <ErrorState message={state.error.error.message} onRetry={load} />
      ) : null}
      {state.status === "success" ? (
        <TableShell caption="Discount categories">
          <thead>
            <tr>
              <Th>Name</Th>
              <Th>Note</Th>
            </tr>
          </thead>
          <tbody>
            {state.data.map((d) => (
              <tr key={d.id}>
                <Td>{d.name}</Td>
                <Td>{d.note || "—"}</Td>
              </tr>
            ))}
          </tbody>
        </TableShell>
      ) : null}
    </>
  );
}
