"use client";

import { EmptyState, ErrorState } from "@fee/ui";
import { useCallback, useEffect, useState } from "react";
import {
  MockBanner,
  PageHeader,
  TableShell,
  TableSkeleton,
  Td,
  Th,
} from "@/components/ui-bits";
import { fetchInstalmentPlans } from "@/lib/mock/client";
import type { InstalmentPlan, LoadState } from "@/lib/types";

export default function InstalmentPlansPage() {
  const [state, setState] = useState<LoadState<InstalmentPlan[]>>({
    status: "loading",
  });

  const load = useCallback(() => {
    setState({ status: "loading" });
    void fetchInstalmentPlans().then(setState);
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  return (
    <>
      <PageHeader
        title="Instalment plans"
        description="How a statement balance splits into payable parts."
      />
      <MockBanner />

      {state.status === "loading" ? <TableSkeleton /> : null}
      {state.status === "empty" ? (
        <EmptyState title="No instalment plans yet." />
      ) : null}
      {state.status === "error" ? (
        <ErrorState message={state.error.error.message} onRetry={load} />
      ) : null}
      {state.status === "success" ? (
        <TableShell caption="Instalment plans">
          <thead>
            <tr>
              <Th>Name</Th>
              <Th>Parts</Th>
              <Th>Note</Th>
            </tr>
          </thead>
          <tbody>
            {state.data.map((p) => (
              <tr key={p.id}>
                <Td>{p.name}</Td>
                <Td>{p.parts}</Td>
                <Td>{p.note}</Td>
              </tr>
            ))}
          </tbody>
        </TableShell>
      ) : null}
    </>
  );
}
