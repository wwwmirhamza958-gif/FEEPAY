/**
 * P0 shell only — Agent F owns feature screens.
 * No wallet, card form, tax UI, or checkout (plan §3.2).
 */
export default function HomePage() {
  return (
    <main className="flex min-h-dvh flex-col items-center justify-center px-[var(--space-16)]">
      <p className="font-heading text-[length:40px] leading-[44px] font-semibold text-ink text-balance">
        Fee Platform
      </p>
      <p className="mt-[var(--space-8)] text-ink-secondary text-sm font-medium">
        Parent web
      </p>
    </main>
  );
}
