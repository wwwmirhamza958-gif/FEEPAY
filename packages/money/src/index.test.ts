import { describe, expect, it } from "vitest";
import {
  add,
  sub,
  formatPkr,
  money,
  moneyFromNumber,
  roundHalfUpPerLine,
  compare,
  assertPkrCurrency,
} from "./index";

describe("money", () => {
  it("constructs from decimal string", () => {
    expect(money("142500.00")).toBe("142500.00");
  });

  it("rejects number constructor", () => {
    expect(() => moneyFromNumber(1)).toThrow(/forbidden/i);
  });

  it("formats whole rupees with thin space", () => {
    expect(formatPkr("142500.00")).toMatch(/Rs[\u2009 ]142,500/);
  });

  it("uses true minus for discounts", () => {
    const s = formatPkr("-35625.00");
    expect(s.startsWith("\u2212")).toBe(true);
  });

  it("rounds half-up per line", () => {
    expect(roundHalfUpPerLine("1.005")).toBe("1.01");
    expect(roundHalfUpPerLine("1.004")).toBe("1.00");
  });

  it("adds and subtracts exactly", () => {
    expect(add("100.00", "50.50")).toBe("150.50");
    expect(sub("100.00", "40.25")).toBe("59.75");
    expect(compare("1.00", "2.00")).toBe(-1);
  });

  it("asserts PKR only", () => {
    expect(() => assertPkrCurrency("USD")).toThrow();
    expect(() => assertPkrCurrency("PKR")).not.toThrow();
  });
});
