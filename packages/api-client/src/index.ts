/**
 * Generated OpenAPI types live in ./generated (CONTRACT section 9.1).
 * Hand-written endpoint DTOs are forbidden — regenerate from openapi.json instead.
 *
 * parseApiError remains from @fee/validation (CONTRACT section 3 / section 6.2).
 */

export type { paths, components, operations, webhooks } from "./generated/schema.js";

import type { components as SchemaComponents } from "./generated/schema.js";

export type ApiError = SchemaComponents["schemas"]["ApiError"];
export type ApiErrorDetail = SchemaComponents["schemas"]["ApiErrorDetail"];
export type ApiErrorEnvelope = SchemaComponents["schemas"]["ApiErrorEnvelope"];

/** Runtime §3 parser — always yields a usable message (never silent/empty). */
export { parseApiError } from "@fee/validation";
