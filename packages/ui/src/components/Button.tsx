import { clsx, type ClassValue } from "clsx";
import {
  forwardRef,
  type ButtonHTMLAttributes,
  type ReactNode,
} from "react";

function cx(...inputs: ClassValue[]) {
  return clsx(inputs);
}

export type ButtonVariant = "primary" | "secondary" | "ghost" | "destructive";
export type ButtonSize = "mobile" | "portal";

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
  size?: ButtonSize;
  loading?: boolean;
  children: ReactNode;
}

const variantClass: Record<ButtonVariant, string> = {
  primary:
    "bg-[var(--ink)] text-[var(--on-ink)] border border-transparent hover:opacity-90",
  secondary:
    "bg-[var(--surface)] text-[var(--ink)] border border-[var(--border-strong)]",
  ghost: "bg-transparent text-[var(--accent)] border border-transparent",
  destructive:
    "bg-transparent text-[var(--danger)] border border-[var(--danger)]",
};

const sizeClass: Record<ButtonSize, string> = {
  mobile: "min-h-[52px] px-[var(--space-20)] text-[16px]",
  portal: "min-h-[44px] px-[var(--space-16)] text-[16px]",
};

/**
 * Button shell — DESIGN_SUPERVISOR §5.1
 * Primary fill is --ink (not accent). Press scale 0.97. Logical padding only.
 */
export const Button = forwardRef<HTMLButtonElement, ButtonProps>(
  function Button(
    {
      variant = "primary",
      size = "mobile",
      loading = false,
      disabled,
      className,
      children,
      type = "button",
      ...rest
    },
    ref
  ) {
    const isDisabled = disabled || loading;
    return (
      <button
        ref={ref}
        type={type}
        disabled={isDisabled}
        aria-busy={loading || undefined}
        className={cx(
          "inline-flex items-center justify-center gap-[var(--space-8)]",
          "rounded-[var(--radius-12)] font-[var(--font-body-strong)]",
          "touch-manipulation select-none",
          "transition-[transform,opacity] duration-[var(--dur-press)] ease-[var(--ease-out)]",
          "active:scale-[0.97]",
          "focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--accent)]",
          "disabled:opacity-50 disabled:pointer-events-none",
          variantClass[variant],
          sizeClass[size],
          className
        )}
        {...rest}
      >
        {loading ? "Processing…" : children}
      </button>
    );
  }
);
