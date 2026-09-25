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
import { fetchFeeSchedules } from "@/lib/mock/client";
import type { FeeScheduleVersion, LoadState } from "@/lib/types";

export default function FeeSchedulesPage() {
  const [state, setState] = useState<LoadState<FeeScheduleVersion[]>>({
    status: "loading",
  });

  const load = useCallback(() => {
    setState({ status: "loading" });
    void fetchFeeSchedules().then(setState);
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  return (
    <>
      <PageHeader
        title="Fee schedules"
        description="Versioned fee schedules. Publishing a new version never silently edits an issued statement."
      />
      <MockBanner />

      {state.status === "loading" ? <TableSkeleton /> : null}
      {state.status === "empty" ? (
        <EmptyState title="No fee schedule versions yet." />
      ) : null}
      {state.status === "error" ? (
        <ErrorState message={state.error.error.message} onRetry={load} />
      ) : null}
      {state.status === "success" ? (
        <TableShell caption="Fee schedule versions">
          <thead>
            <tr>
              <Th>Name</Th>
              <Th>Version</Th>
              <Th>Effective from</Th>
              <Th>Status</Th>
            </tr>
          </thead>
          <tbody>
            {state.data.map((s) => (
              <tr key={s.id}>
                <Td>{s.name}</Td>
                <Td>v{s.version}</Td>
                <Td>{s.effectiveFrom}</Td>
                <Td>{s.status}</Td>
              </tr>
            ))}
          </tbody>
        </TableShell>
      ) : null}
    </>
  );
}
