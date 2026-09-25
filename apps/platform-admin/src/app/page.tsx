/**
 * P0 shell only — platform admin feature screens come later.
 */
export default function HomePage() {
  return (
    <main className="flex min-h-dvh flex-col items-center justify-center px-[var(--space-16)]">
      <p className="font-heading text-[length:40px] leading-[44px] font-semibold text-ink text-balance">
        Fee Platform
      </p>
      <p className="mt-[var(--space-8)] text-ink-secondary text-sm font-medium">
        Platform admin
      </p>
    </main>
  );
}
