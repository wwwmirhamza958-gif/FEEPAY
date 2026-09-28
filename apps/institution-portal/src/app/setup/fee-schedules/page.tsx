"use client";

import { EmptyState, ErrorState } from "@fee/ui";
import { CircleDashed } from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import {
  MockBanner,
  PageHeader,
  TableShell,
  TableSkeleton,
  Td,
  Th,
} from "@/components/ui-bits";
import { fetchFeeSchedules } from "@/lib/api/client";
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
                <Td>
                  {/* TODO: use StatusPill Draft when @fee/ui exports it (Agent A). */}
                  <span
                    className="inline-flex items-center gap-[var(--space-4)] rounded-fee-8 bg-surface-sunken px-[var(--space-8)] py-[var(--space-4)] text-[14px] font-medium text-ink-secondary"
                    role="status"
                  >
                    <CircleDashed size={16} strokeWidth={1.75} aria-hidden="true" />
                    {s.status}
                  </span>
                </Td>
              </tr>
            ))}
          </tbody>
        </TableShell>
      ) : null}
    </>
  );
}
