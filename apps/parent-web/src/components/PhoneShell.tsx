"use client";

import { clsx, type ClassValue } from "clsx";
import {
  FileText,
  History,
  Home,
  LogOut,
  Menu,
  X,
} from "lucide-react";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import {
  useEffect,
  useState,
  type ReactNode,
} from "react";
import { clearParentSession } from "@/lib/parent/client";

export function cx(...inputs: ClassValue[]) {
  return clsx(inputs);
}

const NAV = [
  { href: "/home", label: "Home", icon: Home },
  { href: "/vouchers", label: "Voucher", icon: FileText },
  { href: "/payments/history", label: "History", icon: History },
] as const;

function navActive(pathname: string, href: string) {
  if (href === "/home") return pathname === "/home" || pathname === "/";
  return pathname === href || pathname.startsWith(`${href}/`);
}

/** Compact product mark for chrome. */
export function ProductMark({
  compact = false,
  light = false,
  subtitle = "Parent fees",
}: {
  compact?: boolean;
  light?: boolean;
  subtitle?: string;
}) {
  return (
    <div className="flex items-center gap-[var(--space-12)]">
      <span
        className={cx(
          "flex shrink-0 items-center justify-center rounded-fee-12 font-[var(--font-label)]",
          compact ? "size-9" : "size-11",
          light
            ? "bg-white/10 text-white ring-1 ring-white/15"
            : "border border-border bg-surface text-ink shadow-fee-1"
        )}
        aria-hidden="true"
      >
        Fee
      </span>
      {!compact ? (
        <div className="min-w-0">
          <p
            className={cx(
              "font-[var(--font-body-strong)]",
              light ? "text-white" : "text-ink"
            )}
          >
            Fee Platform
          </p>
          <p
            className={cx(
              "font-[var(--font-micro)]",
              light ? "text-white/60" : "text-ink-tertiary"
            )}
          >
            {subtitle}
          </p>
        </div>
      ) : null}
    </div>
  );
}

/** Login / guest: centred phone frame, no chrome. */
export function PhoneShell({
  children,
  className,
}: {
  children: ReactNode;
  className?: string;
}) {
  return (
    <div
      className={cx(
        "fee-parent-atmosphere mx-auto flex min-h-dvh w-full max-w-[480px] flex-col text-ink",
        className
      )}
    >
      {children}
    </div>
  );
}

/** Authenticated app: header + desktop sidebar + mobile bottom nav. */
export function ParentAppShell({
  children,
  title,
  subtitle,
  hideChrome = false,
}: {
  children: ReactNode;
  title?: string;
  subtitle?: string;
  /** Pay / status flows: keep header, hide sidebar + bottom tabs. */
  hideChrome?: boolean;
}) {
  const pathname = usePathname() ?? "/home";
  const router = useRouter();
  const [drawerOpen, setDrawerOpen] = useState(false);

  useEffect(() => {
    setDrawerOpen(false);
  }, [pathname]);

  function signOut() {
    clearParentSession();
    router.push("/login");
  }

  if (hideChrome) {
    return (
      <div className="fee-parent-atmosphere flex min-h-dvh w-full flex-col text-ink">
        <header className="fee-app-header sticky top-0 z-[var(--z-sticky-header)]">
          <div className="mx-auto flex h-14 max-w-5xl items-center gap-[var(--space-12)] px-[var(--space-16)]">
            <button
              type="button"
              className="min-h-11 font-[var(--font-label)] text-ink-secondary touch-manipulation"
              onClick={() => router.push("/home")}
            >
              ← Home
            </button>
            {title ? (
              <p className="truncate font-[var(--font-body-strong)] text-ink">
                {title}
              </p>
            ) : null}
          </div>
        </header>
        <div className="mx-auto flex w-full max-w-5xl flex-1 flex-col">
          {children}
        </div>
      </div>
    );
  }

  return (
    <div className="fee-parent-atmosphere flex min-h-dvh w-full text-ink">
      {/* Desktop sidebar */}
      <aside className="fee-app-sidebar fixed inset-y-0 left-0 z-30 hidden w-[240px] flex-col lg:flex">
        <div className="border-b border-white/10 px-[var(--space-20)] py-[var(--space-20)]">
          <ProductMark light />
        </div>
        <nav
          className="flex flex-1 flex-col gap-[var(--space-4)] p-[var(--space-12)]"
          aria-label="Main"
        >
          {NAV.map(({ href, label, icon: Icon }) => {
            const active = navActive(pathname, href);
            return (
              <Link
                key={href}
                href={href}
                className={cx(
                  "flex min-h-11 items-center gap-[var(--space-12)] rounded-fee-12 px-[var(--space-12)] font-[var(--font-body-strong)] touch-manipulation transition-colors duration-fee-base ease-fee-out",
                  active
                    ? "bg-white/12 text-white"
                    : "text-white/65 hover:bg-white/8 hover:text-white"
                )}
                aria-current={active ? "page" : undefined}
              >
                <Icon size={18} aria-hidden />
                {label}
              </Link>
            );
          })}
        </nav>
        <div className="border-t border-white/10 p-[var(--space-12)]">
          <button
            type="button"
            onClick={signOut}
            className="flex min-h-11 w-full items-center gap-[var(--space-12)] rounded-fee-12 px-[var(--space-12)] font-[var(--font-body-strong)] text-white/65 touch-manipulation hover:bg-white/8 hover:text-white"
          >
            <LogOut size={18} aria-hidden />
            Sign out
          </button>
        </div>
      </aside>

      {/* Mobile drawer */}
      {drawerOpen ? (
        <div className="fixed inset-0 z-40 lg:hidden" role="dialog" aria-modal="true">
          <button
            type="button"
            className="absolute inset-0 bg-ink/40"
            aria-label="Close menu"
            onClick={() => setDrawerOpen(false)}
          />
          <aside className="fee-app-sidebar absolute inset-y-0 left-0 flex w-[min(280px,86vw)] flex-col shadow-fee-3">
            <div className="flex items-center justify-between border-b border-white/10 px-[var(--space-16)] py-[var(--space-16)]">
              <ProductMark light />
              <button
                type="button"
                className="flex size-11 items-center justify-center rounded-fee-12 text-white/80 touch-manipulation"
                aria-label="Close"
                onClick={() => setDrawerOpen(false)}
              >
                <X size={20} />
              </button>
            </div>
            <nav className="flex flex-1 flex-col gap-[var(--space-4)] p-[var(--space-12)]">
              {NAV.map(({ href, label, icon: Icon }) => {
                const active = navActive(pathname, href);
                return (
                  <Link
                    key={href}
                    href={href}
                    className={cx(
                      "flex min-h-11 items-center gap-[var(--space-12)] rounded-fee-12 px-[var(--space-12)] font-[var(--font-body-strong)]",
                      active ? "bg-white/12 text-white" : "text-white/70"
                    )}
                  >
                    <Icon size={18} aria-hidden />
                    {label}
                  </Link>
                );
              })}
            </nav>
            <div className="border-t border-white/10 p-[var(--space-12)]">
              <button
                type="button"
                onClick={signOut}
                className="flex min-h-11 w-full items-center gap-[var(--space-12)] rounded-fee-12 px-[var(--space-12)] font-[var(--font-body-strong)] text-white/70"
              >
                <LogOut size={18} aria-hidden />
                Sign out
              </button>
            </div>
          </aside>
        </div>
      ) : null}

      <div className="flex min-h-dvh w-full flex-1 flex-col lg:pl-[240px]">
        <header className="fee-app-header sticky top-0 z-[var(--z-sticky-header)]">
          <div className="mx-auto flex h-14 max-w-5xl items-center justify-between gap-[var(--space-12)] px-[var(--space-16)]">
            <div className="flex min-w-0 items-center gap-[var(--space-8)]">
              <button
                type="button"
                className="flex size-11 items-center justify-center rounded-fee-12 border border-border bg-surface text-ink touch-manipulation lg:hidden"
                aria-label="Open menu"
                onClick={() => setDrawerOpen(true)}
              >
                <Menu size={18} />
              </button>
              <div className="min-w-0">
                <p className="truncate font-[var(--font-body-strong)] text-ink">
                  {title ?? "Fee Platform"}
                </p>
                {subtitle ? (
                  <p className="truncate font-[var(--font-micro)] text-ink-tertiary">
                    {subtitle}
                  </p>
                ) : null}
              </div>
            </div>
            <div className="hidden sm:block lg:hidden">
              <ProductMark compact />
            </div>
            <button
              type="button"
              className="flex size-11 items-center justify-center rounded-fee-12 border border-border bg-surface text-ink-secondary touch-manipulation lg:hidden"
              aria-label="Sign out"
              onClick={signOut}
            >
              <LogOut size={18} />
            </button>
          </div>
        </header>

        <main className="mx-auto flex w-full max-w-5xl flex-1 flex-col pb-[calc(4.5rem+env(safe-area-inset-bottom))] lg:pb-[var(--space-32)]">
          {children}
        </main>

        {/* Mobile bottom tabs */}
        <nav
          className="fee-app-bottom-nav fixed inset-x-0 bottom-0 z-[var(--z-sticky-action)] lg:hidden"
          aria-label="Primary"
        >
          <div className="mx-auto grid max-w-5xl grid-cols-3 px-[var(--space-8)] pb-[env(safe-area-inset-bottom)] pt-[var(--space-4)]">
            {NAV.map(({ href, label, icon: Icon }) => {
              const active = navActive(pathname, href);
              return (
                <Link
                  key={href}
                  href={href}
                  className={cx(
                    "flex min-h-[52px] flex-col items-center justify-center gap-[var(--space-2)] rounded-fee-12 font-[var(--font-micro)] touch-manipulation",
                    active ? "text-accent" : "text-ink-tertiary"
                  )}
                  aria-current={active ? "page" : undefined}
                >
                  <span
                    className={cx(
                      "flex size-9 items-center justify-center rounded-fee-12",
                      active ? "bg-accent-soft" : "bg-transparent"
                    )}
                  >
                    <Icon size={18} aria-hidden />
                  </span>
                  {label}
                </Link>
              );
            })}
          </div>
        </nav>
      </div>
    </div>
  );
}

/** Sticky primary CTA — sits above mobile bottom nav (unless flush). */
export function StickyActionBar({
  children,
  className,
  flush = false,
}: {
  children: ReactNode;
  className?: string;
  /** When true (pay / status), stick to viewport bottom — no bottom-nav offset. */
  flush?: boolean;
}) {
  return (
    <div
      className={cx(
        "sticky z-[var(--z-sticky-action)] border-t border-border bg-surface/95 backdrop-blur-sm shadow-fee-2",
        flush
          ? "bottom-0"
          : "bottom-[calc(3.5rem+env(safe-area-inset-bottom))] lg:bottom-0",
        "px-[var(--space-16)] pt-[var(--space-12)]",
        "pb-[calc(var(--space-12)+env(safe-area-inset-bottom))]",
        className
      )}
    >
      <div className="mx-auto w-full max-w-5xl">{children}</div>
    </div>
  );
}

export function MockBanner() {
  if (process.env.NEXT_PUBLIC_PARENT_DATA !== "mock") return null;
  return (
    <p
      className="bg-surface-sunken px-[var(--space-16)] py-[var(--space-8)] text-center font-[var(--font-micro)] text-ink-tertiary"
      role="note"
    >
      Mock data — NEXT_PUBLIC_PARENT_DATA=mock
    </p>
  );
}
