import { parseApiError, type ApiErrorEnvelope } from "@fee/api-client";
import { getApiBaseUrl, getTenantId } from "./config";

export class ApiRequestError extends Error {
  readonly envelope: ApiErrorEnvelope;
  readonly status: number;

  constructor(status: number, envelope: ApiErrorEnvelope) {
    super(envelope.error.message);
    this.name = "ApiRequestError";
    this.status = status;
    this.envelope = envelope;
  }
}

type RequestOptions = {
  method?: string;
  body?: unknown;
  formData?: FormData;
  query?: Record<string, string | undefined>;
  signal?: AbortSignal;
};

function buildUrl(path: string, query?: Record<string, string | undefined>): string {
  const base = getApiBaseUrl();
  const normalized = path.startsWith("/") ? path : `/${path}`;
  const url = new URL(`${base}${normalized}`, typeof window !== "undefined" ? window.location.origin : "http://localhost");
  if (query) {
    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined && value !== "") url.searchParams.set(key, value);
    }
  }
  // Relative path for browser so rewrite stays same-origin
  if (typeof window !== "undefined") {
    return `${url.pathname}${url.search}`;
  }
  return url.toString();
}

export async function apiFetch<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const headers: Record<string, string> = {
    "X-Tenant-Id": getTenantId(),
    Accept: "application/json",
  };

  let body: BodyInit | undefined;
  if (options.formData) {
    body = options.formData;
  } else if (options.body !== undefined) {
    headers["Content-Type"] = "application/json";
    body = JSON.stringify(options.body);
  }

  const res = await fetch(buildUrl(path, options.query), {
    method: options.method ?? (options.body || options.formData ? "POST" : "GET"),
    headers,
    body,
    signal: options.signal,
  });

  if (res.status === 204) {
    return undefined as T;
  }

  const text = await res.text();
  let json: unknown = null;
  if (text) {
    try {
      json = JSON.parse(text) as unknown;
    } catch {
      json = { message: text };
    }
  }

  if (!res.ok) {
    throw new ApiRequestError(res.status, parseApiError(json));
  }

  return json as T;
}

export function toLoadError(err: unknown): ApiErrorEnvelope {
  if (err instanceof ApiRequestError) return err.envelope;
  if (err instanceof TypeError) {
    return parseApiError({
      error: {
        code: "NETWORK_ERROR",
        message: "Could not reach the API. Check that the platform is running.",
        messageKey: "errors.network",
        correlationId: "00000000-0000-0000-0000-000000000000",
        details: [],
        retryable: true,
      },
    });
  }
  return parseApiError(err);
}
