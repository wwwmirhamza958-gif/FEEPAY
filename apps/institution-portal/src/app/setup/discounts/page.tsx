import { PageHeader, Panel } from "@/components/ui-bits";

/**
 * Discount categories — note-only in P1 until C exposes stacking policy APIs.
 * No tax screens (CONTRACT §8.2).
 */
export default function DiscountsPage() {
  return (
    <>
      <PageHeader
        title="Discount categories"
        description="Categories and stacking policy will be configured here once Billing APIs land."
      />
      <Panel className="max-w-2xl">
        <p className="text-[15px] text-ink-secondary">
          Planned: named discount categories, eligibility rules, and stacking order.
          Portal will POST to Agent C — no local EF tables.
        </p>
        <ul className="mt-[var(--space-16)] list-disc ps-[var(--space-20)] text-[15px] text-ink">
          <li>Sibling / staff / merit categories (tenant-defined)</li>
          <li>Stacking policy note shown at preview time</li>
          <li>Amounts formatted via <code>@fee/money</code></li>
        </ul>
        <p className="mt-[var(--space-16)] text-[14px] text-ink-tertiary">
          TODO: GET/POST /api/v1/billing/discount-categories — see REQUESTS_TO_C.md
        </p>
      </Panel>
    </>
  );
}
