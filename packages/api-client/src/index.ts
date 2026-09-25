/**
 * Placeholder for the OpenAPI-generated client (CONTRACT §6.3 / §9).
 *
 * FORBIDDEN: hand-written API request/response DTOs for endpoints.
 * Until generation runs, only the CONTRACT §3 error type is re-exported
 * from @fee/validation (shared parse surface — not a hand-written endpoint shape).
 */

export type {
  ApiErrorEnvelope,
  ApiError,
  ApiErrorDetail,
} from "@fee/validation";

export { parseApiError } from "@fee/validation";

export const API_CLIENT_PLACEHOLDER = true as const;

export function assertGeneratedClient(): never {
  throw new Error(
    "@fee/api-client: no generated client yet. Run the OpenAPI generate step (see README)."
  );
}
