"use client";

import { Search, UserRound } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { MOCK_TENANT_NAME } from "@/lib/mock/data";

export function TopBar() {
  const [menuOpen, setMenuOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);
  const searchRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "/" && !(e.target instanceof HTMLInputElement) && !(e.target instanceof HTMLTextAreaElement)) {
        e.preventDefault();
        searchRef.current?.focus();
      }
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, []);

  useEffect(() => {
    if (!menuOpen) return;
    const onDoc = (e: MouseEvent) => {
      if (!menuRef.current?.contains(e.target as Node)) setMenuOpen(false);
    };
    document.addEventListener("mousedown", onDoc);
    return () => document.removeEventListener("mousedown", onDoc);
  }, [menuOpen]);

  return (
    <header className="sticky top-0 z-fee-header flex h-[56px] items-center gap-[var(--space-16)] border-b border-border bg-surface px-[var(--space-16)]">
      <div className="fee-tenant-chip flex shrink-0 items-center justify-center text-[12px] font-semibold text-ink">
        PU
      </div>
      <div className="min-w-0">
        <p className="truncate text-[14px] font-semibold text-ink">{MOCK_TENANT_NAME}</p>
        <p className="truncate text-[12px] text-ink-tertiary">Tenant placeholder</p>
      </div>

      <div className="ms-auto flex min-w-0 flex-1 items-center justify-end gap-[var(--space-12)] max-w-xl">
        <label className="relative w-full max-w-sm">
          <span className="sr-only">Search</span>
          <Search
            size={16}
            strokeWidth={1.75}
            aria-hidden="true"
            className="pointer-events-none absolute start-[12px] top-1/2 -translate-y-1/2 text-ink-tertiary"
          />
          <input
            ref={searchRef}
            type="search"
            placeholder="Search students, statements… (/)"
            className="w-full rounded-fee-8 border border-border-strong bg-surface-sunken py-[var(--space-8)] pe-[var(--space-12)] ps-[var(--space-32)] text-[14px] text-ink placeholder:text-ink-tertiary focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
          />
        </label>

        <div className="relative" ref={menuRef}>
          <button
            type="button"
            aria-haspopup="menu"
            aria-expanded={menuOpen}
            onClick={() => setMenuOpen((v) => !v)}
            className="inline-flex size-11 items-center justify-center rounded-fee-8 text-ink transition-[transform] duration-fee-press ease-fee-out active:scale-[0.97] hover:bg-surface-sunken focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
            aria-label="User menu"
          >
            <UserRound size={20} strokeWidth={1.75} aria-hidden="true" />
          </button>
          {menuOpen ? (
            <div
              role="menu"
              className="absolute end-0 top-[calc(100%+4px)] z-fee-sheet min-w-[200px] rounded-fee-12 border border-border bg-surface p-[var(--space-8)] shadow-fee-2"
            >
              <p className="px-[var(--space-8)] py-[var(--space-4)] text-[12px] text-ink-tertiary">
                Finance Admin
              </p>
              <button
                type="button"
                role="menuitem"
                className="w-full rounded-fee-8 px-[var(--space-8)] py-[var(--space-8)] text-start text-[14px] text-ink hover:bg-surface-sunken focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
                onClick={() => setMenuOpen(false)}
              >
                Profile (soon)
              </button>
              <button
                type="button"
                role="menuitem"
                className="w-full rounded-fee-8 px-[var(--space-8)] py-[var(--space-8)] text-start text-[14px] text-danger hover:bg-danger-soft focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
                onClick={() => setMenuOpen(false)}
              >
                Sign out
              </button>
            </div>
          ) : null}
        </div>
      </div>
    </header>
  );
}
