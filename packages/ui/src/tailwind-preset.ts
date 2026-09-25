/**
 * Tailwind theme extension mapping DESIGN_SUPERVISOR tokens.
 * Apps merge this into tailwind.config — never redefine colours as hex in apps.
 */
export const feeTailwindTheme = {
  extend: {
    colors: {
      canvas: "var(--canvas)",
      surface: "var(--surface)",
      "surface-sunken": "var(--surface-sunken)",
      ink: "var(--ink)",
      "ink-secondary": "var(--ink-secondary)",
      "ink-tertiary": "var(--ink-tertiary)",
      border: "var(--border)",
      "border-strong": "var(--border-strong)",
      accent: "var(--accent)",
      "accent-soft": "var(--accent-soft)",
      success: "var(--success)",
      "success-soft": "var(--success-soft)",
      warning: "var(--warning)",
      "warning-soft": "var(--warning-soft)",
      danger: "var(--danger)",
      "danger-soft": "var(--danger-soft)",
      "on-ink": "var(--on-ink)",
    },
    borderRadius: {
      "fee-8": "var(--radius-8)",
      "fee-12": "var(--radius-12)",
      "fee-16": "var(--radius-16)",
      "fee-24": "var(--radius-24)",
    },
    boxShadow: {
      "fee-1": "var(--shadow-1)",
      "fee-2": "var(--shadow-2)",
      "fee-3": "var(--shadow-3)",
    },
    zIndex: {
      "fee-base": "var(--z-base)",
      "fee-header": "var(--z-sticky-header)",
      "fee-action": "var(--z-sticky-action)",
      "fee-sheet": "var(--z-sheet)",
      "fee-dialog": "var(--z-dialog)",
      "fee-toast": "var(--z-toast)",
    },
    transitionTimingFunction: {
      "fee-out": "var(--ease-out)",
      "fee-in-out": "var(--ease-in-out)",
      "fee-drawer": "var(--ease-drawer)",
    },
    transitionDuration: {
      "fee-press": "var(--dur-press)",
      "fee-fast": "var(--dur-fast)",
      "fee-base": "var(--dur-base)",
      "fee-sheet": "var(--dur-sheet)",
      "fee-exit": "var(--dur-exit)",
    },
    fontFamily: {
      heading: "var(--font-heading)",
      body: "var(--font-body)",
      urdu: "var(--font-urdu)",
    },
  },
} as const;

export default feeTailwindTheme;
