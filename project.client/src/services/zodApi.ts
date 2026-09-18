import type { AxiosResponse, RawAxiosRequestHeaders } from "axios";
import type { z } from "zod";

import {
  api,
  clearToken,
  readToken,
  setAuthorization,
} from "../configs/axios/interceptors";
import type { ApiResponse } from "../types/ApiResponse";
import { apiEnvelopeSchema } from "../types/schemas";

/**
 * Phase 4.9 — validated API helper.
 *
 * Replaces the unsafe `api.get<unknown, ApiResponse<T>>(url)` pattern with a
 * Zod-validated round-trip. The envelope is parsed first; `data` is then
 * refined by the caller's schema. Validation failures raise `SchemaError`.
 *
 * Return shape stays compatible with the existing `ApiResponse<T>` so
 * call sites can swap implementations without changing downstream code.
 */

export class SchemaError extends Error {
  override readonly name = "SchemaError";
  readonly issues: ReadonlyArray<{
    readonly path: string;
    readonly message: string;
  }>;
  constructor(
    message: string,
    issues: ReadonlyArray<{ readonly path: string; readonly message: string }>,
  ) {
    super(message);
    this.issues = issues;
  }
}

function flattenIssues(error: z.ZodError): Array<{
  readonly path: string;
  readonly message: string;
}> {
  return error.issues.map((issue) => ({
    path: issue.path.join(".") || "(root)",
    message: issue.message,
  }));
}

function envelopeFrom<T>(
  envelope: { success: boolean; message: string; totalResults: number },
  data: T,
): ApiResponse<T> {
  if (envelope.success) {
    return {
      success: true,
      data,
      message: envelope.message,
      totalResults: envelope.totalResults,
    };
  }
  return {
    success: false,
    data: null,
    message: envelope.message,
    totalResults: envelope.totalResults,
  };
}

interface ParsedEnvelope {
  readonly success: boolean;
  readonly message: string;
  readonly totalResults: number;
  readonly data: unknown;
}

function parseEnvelopeInternal(raw: unknown): ParsedEnvelope {
  const envelope = apiEnvelopeSchema.safeParse(raw);
  if (!envelope.success) {
    throw new SchemaError(
      "API envelope did not match the expected shape.",
      flattenIssues(envelope.error),
    );
  }
  return {
    success: envelope.data.success,
    message: envelope.data.message,
    totalResults: envelope.data.totalResults ?? 0,
    data: envelope.data.data,
  };
}

function parseData<T>(raw: unknown, dataSchema: z.ZodType<T>): T {
  const inner = dataSchema.safeParse(raw);
  if (!inner.success) {
    throw new SchemaError(
      "API response data did not match the expected shape.",
      flattenIssues(inner.error),
    );
  }
  return inner.data;
}

/**
 * Standalone helper for endpoints that go through `fetch` directly (e.g. the
 * cookie-bearing `/Auth/Refresh` bootstrap). Parses a JSON body through the
 * envelope schema + a caller-supplied data schema; returns the same
 * `ApiResponse<T>` shape the rest of `zodApi` produces.
 */
export function parseEnvelope<T>(
  raw: unknown,
  dataSchema: z.ZodType<T>,
): ApiResponse<T> {
  const env = parseEnvelopeInternal(raw);
  const data = parseData<T>(env.data, dataSchema);
  return envelopeFrom(env, data);
}

/**
 * Variant that returns `null` on validation failure instead of throwing.
 * Useful for non-blocking hydration paths (e.g. `useAuthStore.syncAuth`)
 * where an invalid response should be ignored rather than crash the app.
 */
export function tryParseEnvelope<T>(
  raw: unknown,
  dataSchema: z.ZodType<T>,
): ApiResponse<T> | null {
  try {
    return parseEnvelope(raw, dataSchema);
  } catch {
    return null;
  }
}

export const zodApi = {
  async get<T>(
    url: string,
    dataSchema: z.ZodType<T>,
    config?: { headers?: RawAxiosRequestHeaders },
  ): Promise<ApiResponse<T>> {
    const response = await api.get(url, config);
    const env = parseEnvelopeInternal(response.data);
    const data = parseData<T>(env.data, dataSchema);
    return envelopeFrom(env, data);
  },

  async post<T, Body = unknown>(
    url: string,
    dataSchema: z.ZodType<T>,
    body?: Body,
    config?: { headers?: RawAxiosRequestHeaders },
  ): Promise<ApiResponse<T>> {
    const response = await api.post<unknown, AxiosResponse, Body>(
      url,
      body,
      config,
    );
    const env = parseEnvelopeInternal(response.data);
    const data = parseData<T>(env.data, dataSchema);
    return envelopeFrom(env, data);
  },

  async put<T, Body = unknown>(
    url: string,
    dataSchema: z.ZodType<T>,
    body?: Body,
    config?: { headers?: RawAxiosRequestHeaders },
  ): Promise<ApiResponse<T>> {
    const response = await api.put<unknown, AxiosResponse, Body>(
      url,
      body,
      config,
    );
    const env = parseEnvelopeInternal(response.data);
    const data = parseData<T>(env.data, dataSchema);
    return envelopeFrom(env, data);
  },

  async patch<T, Body = unknown>(
    url: string,
    dataSchema: z.ZodType<T>,
    body?: Body,
    config?: { headers?: RawAxiosRequestHeaders },
  ): Promise<ApiResponse<T>> {
    const response = await api.patch<unknown, AxiosResponse, Body>(
      url,
      body,
      config,
    );
    const env = parseEnvelopeInternal(response.data);
    const data = parseData<T>(env.data, dataSchema);
    return envelopeFrom(env, data);
  },

  async delete<T>(
    url: string,
    dataSchema: z.ZodType<T>,
    config?: { headers?: RawAxiosRequestHeaders },
  ): Promise<ApiResponse<T>> {
    const response = await api.delete(url, config);
    const env = parseEnvelopeInternal(response.data);
    const data = parseData<T>(env.data, dataSchema);
    return envelopeFrom(env, data);
  },
};

/**
 * Re-export the token helpers so service files can use a single import.
 */
export { api, clearToken, readToken, setAuthorization };
