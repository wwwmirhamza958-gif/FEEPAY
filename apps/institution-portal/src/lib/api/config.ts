/** Pilot tenant from CONTRACT / IdentityTenancy PilotSeed. */
export const PILOT_TENANT_ID = "11111111-1111-1111-1111-111111111111";

export const FALL_2026_TERM_ID = "44444444-4444-4444-4444-444444440001";

/** When `mock`, portal keeps using `src/lib/mock/client`. Default is live API. */
export function useMockData(): boolean {
  return process.env.NEXT_PUBLIC_PORTAL_DATA === "mock";
}

/**
 * Upstream API origin (used by next.config rewrite destination and SSR).
 * Browser calls go through `/api/platform` to avoid CORS.
 */
export function getUpstreamApiBaseUrl(): string {
  return (
    process.env.NEXT_PUBLIC_API_BASE_URL?.replace(/\/$/, "") ||
    "http://localhost:5137"
  );
}

/** Base URL used by the fetch client. */
export function getApiBaseUrl(): string {
  if (typeof window !== "undefined") {
    return "/api/platform";
  }
  return getUpstreamApiBaseUrl();
}

export function getTenantId(): string {
  return process.env.NEXT_PUBLIC_TENANT_ID || PILOT_TENANT_ID;
}
