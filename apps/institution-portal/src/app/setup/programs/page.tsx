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
import { createProgram, fetchPrograms } from "@/lib/mock/client";
import type { LoadState, Program } from "@/lib/types";

export default function ProgramsPage() {
  const [state, setState] = useState<LoadState<Program[]>>({ status: "loading" });
  const [code, setCode] = useState("");
  const [name, setName] = useState("");
  const [intake, setIntake] = useState("Fall 2026");
  const [saving, setSaving] = useState(false);

  const load = useCallback(() => {
    setState({ status: "loading" });
    void fetchPrograms().then(setState);
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  async function onCreate() {
    if (!code.trim() || !name.trim()) return;
    setSaving(true);
    const res = await createProgram({
      code: code.trim(),
      name: name.trim(),
      intakeLabel: intake.trim(),
    });
    setSaving(false);
    if (!res.ok) {
      setState({ status: "error", error: res.error });
      return;
    }
    setCode("");
    setName("");
    load();
  }

  return (
    <>
      <PageHeader
        title="Programs & intakes"
        description="Canonical academic programs and intake labels used by fee schedules."
        primaryAction={
          <Button
            size="portal"
            onClick={() => void onCreate()}
            loading={saving}
            disabled={!code.trim() || !name.trim()}
          >
            Add program
          </Button>
        }
      />
      <MockBanner />

      <Panel className="mb-[var(--space-24)] max-w-2xl">
        <div className="grid gap-[var(--space-12)] sm:grid-cols-3">
          <div>
            <FieldLabel htmlFor="prog-code">Code</FieldLabel>
            <TextInput id="prog-code" value={code} onChange={(e) => setCode(e.target.value)} />
          </div>
          <div className="sm:col-span-2">
            <FieldLabel htmlFor="prog-name">Name</FieldLabel>
            <TextInput id="prog-name" value={name} onChange={(e) => setName(e.target.value)} />
          </div>
          <div className="sm:col-span-3">
            <FieldLabel htmlFor="prog-intake">Intake</FieldLabel>
            <TextInput
              id="prog-intake"
              value={intake}
              onChange={(e) => setIntake(e.target.value)}
            />
          </div>
        </div>
        <p className="mt-[var(--space-8)] text-[14px] text-ink-tertiary">
          TODO: POST /api/v1/academic/programs
        </p>
      </Panel>

      {state.status === "loading" ? <TableSkeleton /> : null}
      {state.status === "empty" ? (
        <EmptyState title="No programs yet. Add the first program above." />
      ) : null}
      {state.status === "error" ? (
        <ErrorState message={state.error.error.message} onRetry={load} />
      ) : null}
      {state.status === "success" ? (
        <TableShell caption="Programs and intakes">
          <thead>
            <tr>
              <Th>Code</Th>
              <Th>Name</Th>
              <Th>Intake</Th>
            </tr>
          </thead>
          <tbody>
            {state.data.map((p) => (
              <tr key={p.id}>
                <Td>{p.code}</Td>
                <Td>{p.name}</Td>
                <Td>{p.intakeLabel}</Td>
              </tr>
            ))}
          </tbody>
        </TableShell>
      ) : null}
    </>
  );
}
