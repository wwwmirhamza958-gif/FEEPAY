"use client";

import { clsx } from "clsx";
import {
  ChevronLeft,
  ChevronRight,
  CircleDollarSign,
  Database,
  LayoutDashboard,
  Settings2,
  type LucideIcon,
} from "lucide-react";
import Link from "next/link";
import { usePathname } from "next/navigation";

export type NavItem = {
  href: string;
  label: string;
  icon: LucideIcon;
};

export type NavSection = {
  title: string;
  items: NavItem[];
};

export const PORTAL_NAV: NavSection[] = [
  {
    title: "Overview",
    items: [{ href: "/", label: "Home", icon: LayoutDashboard }],
  },
  {
    title: "Setup",
    items: [
      { href: "/setup/branding", label: "Branding", icon: Settings2 },
      { href: "/setup/programs", label: "Programs & intakes", icon: Settings2 },
      { href: "/setup/fee-heads", label: "Fee heads", icon: Settings2 },
      { href: "/setup/fee-schedules", label: "Fee schedules", icon: Settings2 },
      { href: "/setup/discounts", label: "Discount categories", icon: Settings2 },
      {
        href: "/setup/instalment-plans",
        label: "Instalment plans",
        icon: Settings2,
      },
    ],
  },
  {
    title: "Data",
    items: [
      { href: "/data/import", label: "Import wizard", icon: Database },
      { href: "/data/students", label: "Student search", icon: Database },
    ],
  },
  {
    title: "Billing",
    items: [
      { href: "/billing/preview", label: "Preview & issue", icon: CircleDollarSign },
      { href: "/billing/adjustments", label: "Adjustments", icon: CircleDollarSign },
    ],
  },
];

export function Sidebar({
  collapsed,
  onToggle,
}: {
  collapsed: boolean;
  onToggle: () => void;
}) {
  const pathname = usePathname();

  return (
    <aside
      className={clsx(
        "sticky top-0 flex h-dvh shrink-0 flex-col border-e border-border bg-surface",
        collapsed ? "w-[64px]" : "w-[240px]"
      )}
      aria-label="Primary"
    >
      <div
        className={clsx(
          "flex h-[56px] items-center border-b border-border px-[var(--space-12)]",
          collapsed ? "justify-center" : "justify-between gap-[var(--space-8)]"
        )}
      >
        {!collapsed ? (
          <span className="font-heading text-[length:16px] font-semibold text-ink truncate">
            Fee Platform
          </span>
        ) : null}
        <button
          type="button"
          onClick={onToggle}
          aria-label={collapsed ? "Expand sidebar" : "Collapse sidebar"}
          aria-expanded={!collapsed}
          className="inline-flex size-11 items-center justify-center rounded-fee-8 text-ink-secondary transition-[transform,opacity] duration-fee-press ease-fee-out active:scale-[0.97] hover:bg-surface-sunken focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
        >
          {collapsed ? (
            <ChevronRight size={20} strokeWidth={1.75} aria-hidden="true" />
          ) : (
            <ChevronLeft size={20} strokeWidth={1.75} aria-hidden="true" />
          )}
        </button>
      </div>

      <nav className="flex-1 overflow-y-auto py-[var(--space-12)]" aria-label="Sections">
        {PORTAL_NAV.map((section) => (
          <div key={section.title} className="mb-[var(--space-16)]">
            {!collapsed ? (
              <p className="px-[var(--space-16)] mb-[var(--space-4)] text-[12px] font-medium uppercase tracking-wide text-ink-tertiary">
                {section.title}
              </p>
            ) : null}
            <ul className="flex flex-col gap-[var(--space-2)] px-[var(--space-8)]">
              {section.items.map((item) => {
                const active =
                  item.href === "/"
                    ? pathname === "/"
                    : pathname === item.href || pathname.startsWith(`${item.href}/`);
                const Icon = item.icon;
                return (
                  <li key={item.href}>
                    <Link
                      href={item.href}
                      title={item.label}
                      aria-current={active ? "page" : undefined}
                      className={clsx(
                        "flex items-center gap-[var(--space-8)] rounded-fee-8 px-[var(--space-8)] py-[var(--space-8)] text-[14px] font-medium",
                        "transition-[background-color,color] duration-fee-fast ease-fee-out",
                        "focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent",
                        collapsed && "justify-center",
                        active
                          ? "bg-accent-soft text-accent"
                          : "text-ink-secondary hover:bg-surface-sunken hover:text-ink"
                      )}
                    >
                      <Icon size={20} strokeWidth={1.75} aria-hidden="true" />
                      {!collapsed ? <span className="truncate">{item.label}</span> : null}
                    </Link>
                  </li>
                );
              })}
            </ul>
          </div>
        ))}
      </nav>
    </aside>
  );
}
