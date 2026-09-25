import Link from "next/link";
import { PageHeader, Panel } from "@/components/ui-bits";

const shortcuts = [
  {
    href: "/setup/branding",
    title: "Setup",
    body: "Branding, programs, fee heads, schedules, instalment plans.",
  },
  {
    href: "/data/import",
    title: "Import data",
    body: "Upload → Map → Dry run → Commit with count tiles.",
  },
  {
    href: "/billing/preview",
    title: "Billing preview",
    body: "Run a preview, review outliers, then issue statements.",
  },
];

export default function HomePage() {
  return (
    <>
      <PageHeader
        title="Institution portal"
        description="Dense desk UI for finance officers. P1 covers Setup, Data import, and Billing preview/issue."
      />
      <div className="grid gap-[var(--space-16)] md:grid-cols-3">
        {shortcuts.map((s) => (
          <Panel key={s.href}>
            <h2 className="text-[length:18px] font-semibold text-ink">{s.title}</h2>
            <p className="mt-[var(--space-8)] text-[15px] text-ink-secondary">{s.body}</p>
            <Link
              href={s.href}
              className="mt-[var(--space-16)] inline-flex text-[14px] font-semibold text-accent focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
            >
              Open
            </Link>
          </Panel>
        ))}
      </div>
    </>
  );
}
