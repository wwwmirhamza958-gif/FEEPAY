"use client";

import { EmptyState, ErrorState } from "@fee/ui";
import { useCallback, useEffect, useState } from "react";
import {
  FieldLabel,
  MockBanner,
  PageHeader,
  TableShell,
  TableSkeleton,
  Td,
  TextInput,
  Th,
} from "@/components/ui-bits";
import { searchStudents } from "@/lib/mock/client";
import type { LoadState, StudentRow } from "@/lib/types";

export default function StudentsPage() {
  const [query, setQuery] = useState("");
  const [state, setState] = useState<LoadState<StudentRow[]>>({ status: "loading" });

  const load = useCallback((q: string) => {
    setState({ status: "loading" });
    void searchStudents(q).then(setState);
  }, []);

  useEffect(() => {
    const t = setTimeout(() => load(query), 300);
    return () => clearTimeout(t);
  }, [query, load]);

  return (
    <>
      <PageHeader
        title="Student search"
        description="Stub list for P1. Student 360 lands after Billing APIs expose enrolments and statements."
      />
      <MockBanner />

      <div className="mb-[var(--space-16)] max-w-md">
        <FieldLabel htmlFor="student-q">Search</FieldLabel>
        <TextInput
          id="student-q"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder="Name or student number"
          autoComplete="off"
        />
      </div>

      {state.status === "loading" ? <TableSkeleton /> : null}
      {state.status === "empty" ? (
        <EmptyState title="No students match that search." />
      ) : null}
      {state.status === "error" ? (
        <ErrorState message={state.error.error.message} onRetry={() => load(query)} />
      ) : null}
      {state.status === "success" ? (
        <TableShell caption="Students">
          <thead>
            <tr>
              <Th>Student number</Th>
              <Th>Name</Th>
              <Th>Program</Th>
              <Th>Intake</Th>
            </tr>
          </thead>
          <tbody>
            {state.data.map((s) => (
              <tr key={s.id}>
                <Td>{s.studentNumber}</Td>
                <Td>{s.fullName}</Td>
                <Td>{s.programCode}</Td>
                <Td>{s.intakeLabel}</Td>
              </tr>
            ))}
          </tbody>
        </TableShell>
      ) : null}
    </>
  );
}
