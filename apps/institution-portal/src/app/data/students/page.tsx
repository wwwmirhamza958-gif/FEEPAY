"use client";

import { Button, EmptyState, ErrorState } from "@fee/ui";
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
import {
  createStudent,
  fetchPrograms,
  fetchTerms,
  searchStudents,
  type AcademicTerm,
} from "@/lib/api/client";
import type { LoadState, Program, StudentRow } from "@/lib/types";

export default function StudentsPage() {
  const [query, setQuery] = useState("");
  const [state, setState] = useState<LoadState<StudentRow[]>>({ status: "loading" });
  const [programs, setPrograms] = useState<Program[]>([]);
  const [terms, setTerms] = useState<AcademicTerm[]>([]);
  const [showForm, setShowForm] = useState(false);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [successMsg, setSuccessMsg] = useState<string | null>(null);

  const [externalId, setExternalId] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [programId, setProgramId] = useState("");
  const [termId, setTermId] = useState("");
  const [fatherMobile, setFatherMobile] = useState("");
  const [fatherName, setFatherName] = useState("");

  const load = useCallback((q: string) => {
    setState({ status: "loading" });
    void searchStudents(q).then(setState);
  }, []);

  useEffect(() => {
    const t = setTimeout(() => load(query), 300);
    return () => clearTimeout(t);
  }, [query, load]);

  useEffect(() => {
    void fetchPrograms().then((res) => {
      if (res.status === "success") {
        setPrograms(res.data);
        if (!programId && res.data[0]) setProgramId(res.data[0].id);
      }
    });
    void fetchTerms().then((res) => {
      if (res.status === "success") {
        setTerms(res.data);
        if (!termId && res.data[0]) setTermId(res.data[0].id);
      }
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps -- init pickers once
  }, []);

  async function onAddStudent(e: React.FormEvent) {
    e.preventDefault();
    setFormError(null);
    setSuccessMsg(null);
    if (!externalId.trim() || !displayName.trim() || !programId || !termId || !fatherMobile.trim()) {
      setFormError("Fill student number, name, program, term, and father phone.");
      return;
    }
    setSaving(true);
    const res = await createStudent({
      externalId: externalId.trim(),
      displayName: displayName.trim(),
      programId,
      termId,
      fatherMobile: fatherMobile.trim(),
      fatherDisplayName: fatherName.trim() || undefined,
    });
    setSaving(false);
    if (!res.ok) {
      setFormError(res.error.error.message);
      return;
    }
    setSuccessMsg(
      `Added ${res.data.displayName}. Challan ${res.data.feeAmount} PKR issued. Parent login: ${res.data.fatherMobileE164} · OTP 123456`
    );
    setExternalId("");
    setDisplayName("");
    setFatherMobile("");
    setFatherName("");
    setShowForm(false);
    load(query);
  }

  return (
    <>
      <PageHeader
        title="Students"
        description="Search students or add a student with father phone — a fee challan is issued and linked for parent login."
        primaryAction={
          <Button
            variant={showForm ? "secondary" : "primary"}
            onClick={() => {
              setShowForm((v) => !v);
              setFormError(null);
            }}
          >
            {showForm ? "Cancel" : "Add Student"}
          </Button>
        }
      />
      <MockBanner />

      {successMsg ? (
        <p
          role="status"
          className="mb-[var(--space-16)] rounded-fee-8 border border-border bg-surface px-[var(--space-12)] py-[var(--space-12)] font-[var(--font-body)] text-ink"
        >
          {successMsg}
        </p>
      ) : null}

      {showForm ? (
        <form
          onSubmit={(e) => void onAddStudent(e)}
          className="mb-[var(--space-24)] max-w-xl space-y-[var(--space-12)] rounded-fee-8 border border-border bg-surface p-[var(--space-16)]"
        >
          <h2 className="font-[var(--font-title-2)] text-ink">Add Student</h2>
          <div>
            <FieldLabel htmlFor="stu-num">Student number / Roll No</FieldLabel>
            <TextInput
              id="stu-num"
              value={externalId}
              onChange={(e) => setExternalId(e.target.value)}
              required
              autoComplete="off"
            />
          </div>
          <div>
            <FieldLabel htmlFor="stu-name">Student name</FieldLabel>
            <TextInput
              id="stu-name"
              value={displayName}
              onChange={(e) => setDisplayName(e.target.value)}
              required
            />
          </div>
          <div>
            <FieldLabel htmlFor="stu-program">Program / Class</FieldLabel>
            <select
              id="stu-program"
              value={programId}
              onChange={(e) => setProgramId(e.target.value)}
              className="mt-[var(--space-4)] min-h-11 w-full rounded-fee-8 border border-border-strong bg-surface px-[var(--space-12)] font-[var(--font-body)] text-ink"
              required
            >
              {programs.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.code} — {p.name}
                </option>
              ))}
            </select>
          </div>
          <div>
            <FieldLabel htmlFor="stu-term">Term (for fee challan)</FieldLabel>
            <select
              id="stu-term"
              value={termId}
              onChange={(e) => setTermId(e.target.value)}
              className="mt-[var(--space-4)] min-h-11 w-full rounded-fee-8 border border-border-strong bg-surface px-[var(--space-12)] font-[var(--font-body)] text-ink"
              required
            >
              {terms.map((t) => (
                <option key={t.id} value={t.id}>
                  {t.code} — {t.name}
                </option>
              ))}
            </select>
          </div>
          <div>
            <FieldLabel htmlFor="father-name">Father / Parent name</FieldLabel>
            <TextInput
              id="father-name"
              value={fatherName}
              onChange={(e) => setFatherName(e.target.value)}
              placeholder="Optional"
            />
          </div>
          <div>
            <FieldLabel htmlFor="father-mobile">Father / Parent phone</FieldLabel>
            <TextInput
              id="father-mobile"
              value={fatherMobile}
              onChange={(e) => setFatherMobile(e.target.value)}
              placeholder="3001234567"
              required
              inputMode="numeric"
            />
            <p className="mt-[var(--space-4)] font-[var(--font-micro)] text-ink-tertiary">
              Parent logs in at parent app with this number and OTP 123456 (Dev).
            </p>
          </div>
          {formError ? (
            <p role="alert" className="font-[var(--font-body)] text-danger">
              {formError}
            </p>
          ) : null}
          <Button type="submit" variant="primary" disabled={saving}>
            {saving ? "Saving…" : "Save & issue challan"}
          </Button>
        </form>
      ) : null}

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
