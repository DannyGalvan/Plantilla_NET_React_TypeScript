import axios, { AxiosError, type InternalAxiosRequestConfig } from "axios";

import type { InitialAuth } from "../../types/InitialAuth";
import {
  ForbiddenError,
  InternalServerError,
  NetworkError,
  UnauthorizedError,
} from "../../types/errors";

/**
 * Base URL is taken from `import.meta.env.VITE_API_URL` and falls back to
 * same-origin when not configured (F11). Anything starting with the base
 * URL is forwarded to the API.
 */
export const API_URL =
  (import.meta.env.VITE_API_URL ?? "").replace(/\/$/, "") + "/api/v1/";

export const api = axios.create({
  baseURL: API_URL,
  headers: {
    "Content-Type": "application/json",
    Accept: "application/json",
  },
});

// ============================================================
// Request interceptor — attach the bearer token from the in-memory
// store (no JWT in localStorage after Phase 4).
// ============================================================
api.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  // The store is the single source of truth. The interceptor pulls it
  // lazily to avoid a circular import at module load.
  const token = readToken();
  if (token) {
    config.headers.set("Authorization", `Bearer ${token}`);
  }

  // 3.9 — double-submit CSRF: echo the XSRF-TOKEN cookie (set by the
  // backend on login/refresh) as the X-XSRF-TOKEN header. The backend
  // /Auth/Refresh endpoint requires the cookie and the header to match.
  const xsrf = readCookie("XSRF-TOKEN");
  if (xsrf) {
    config.headers.set("X-XSRF-TOKEN", xsrf);
  }

  return config;
});

function readCookie(name: string): string | null {
  if (typeof document === "undefined") return null;
  const target = `${name}=`;
  const parts = document.cookie.split(";");
  for (const raw of parts) {
    const cookie = raw.trim();
    if (cookie.startsWith(target)) {
      return decodeURIComponent(cookie.substring(target.length));
    }
  }
  return null;
}

// ============================================================
// Response interceptor — surface typed errors, never raw exceptions.
// ============================================================
api.interceptors.response.use(
  async (response) => response.data,
  (error: AxiosError) => {
    // F8 (a): guard against network failures (no `error.response`).
    if (!error.response) {
      throw new NetworkError(
        "No se pudo contactar al servidor. Verifique su conexión.",
      );
    }

    const { status, data } = error.response;

    if (status === 401) {
      // Token expired / invalid — clear the in-memory store.
      clearToken();
      throw new UnauthorizedError(
        "Tu sesión ha expirado, vuelve a iniciar sesión.",
      );
    }

    if (status === 403) {
      throw new ForbiddenError("No tienes permisos para realizar esta acción.");
    }

    if (status === 500) {
      throw new InternalServerError(
        "Hubo un error en el servidor. Notifica al desarrollador.",
      );
    }

    // 4xx (other than 401/403): surface the server envelope as a typed error
    // — never as a "successful" payload.
    throw new Error(
      (data as { message?: string } | undefined)?.message ??
        `Request failed with status ${status}.`,
    );
  },
);

// ============================================================
// Token plumbing — in-memory only (F1: no JWT in localStorage).
// ============================================================

let currentToken: string | null = null;

export const setAuthorization = (token: string | null | undefined) => {
  // F8 (d): the previous guard `token !== undefined || token !== null` was
  // always true (||, not &&). Now we accept undefined/null/empty as
  // "clear the token".
  if (typeof token !== "string" || token.length === 0) {
    currentToken = null;
    delete api.defaults.headers.common.Authorization;
    return;
  }
  currentToken = token;
  api.defaults.headers.common.Authorization = `Bearer ${token}`;
};

export const readToken = (): string | null => currentToken;

/**
 * Called by the response interceptor when a 401 arrives. The store calls
 * `setAuthorization(null)` so the next request goes out anonymously.
 */
export const clearToken = () => setAuthorization(null);

// ============================================================
// One-shot localStorage rehydration (defence in depth). The full
// no-JWT-in-localStorage migration is gated on Phase 3.8
// (refresh tokens + HttpOnly cookie); until then we still
// tolerate re-hydrating from the legacy `@auth` blob but never
// WRITE to it.
// ============================================================
export const rehydrateLegacyAuth = (): InitialAuth | null => {
  if (typeof window === "undefined") return null;
  const raw = window.localStorage.getItem("@auth");
  if (!raw) return null;
  try {
    return JSON.parse(raw) as InitialAuth;
  } catch {
    // F8 (c): swallow JSON parse errors instead of throwing.
    return null;
  }
};
