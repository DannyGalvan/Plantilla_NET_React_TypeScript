import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios';

import { useAuthStore } from '@/shared/auth/auth-store';
import { sessionSchema } from '@/shared/auth/types';
import { env } from '@/shared/config/env';

import { ApiError } from './problem-details';

const XSRF_COOKIE = 'XSRF-TOKEN';
const XSRF_HEADER = 'X-XSRF-TOKEN';

/** Endpoints that must never trigger the refresh-and-retry flow. */
const AUTH_ENDPOINTS = ['/identity/auth/login', '/identity/auth/refresh', '/identity/auth/logout'];

type RetriableConfig = InternalAxiosRequestConfig & { _retried?: boolean };

export function readCookie(name: string): string | null {
  const match = document.cookie.split('; ').find((row) => row.startsWith(`${name}=`));
  return match ? decodeURIComponent(match.slice(name.length + 1)) : null;
}

/** Hooks the app plugs in (language for Accept-Language, what to do when the session ends). */
export const httpHooks = {
  language: (): string => 'es',
  onSessionExpired: (): void => undefined,
};

/** Plain client for the refresh call itself (no interceptors, no recursion). */
const raw = axios.create({ baseURL: env.VITE_API_BASE_URL, withCredentials: true });

/** Shared API client: bearer token, CSRF header, Accept-Language, refresh-on-401 and ProblemDetails errors. */
export const http = axios.create({ baseURL: env.VITE_API_BASE_URL, withCredentials: true });

let refreshing: Promise<string | null> | null = null;

/**
 * Refreshes the access token once, even if many requests fail with 401 at the same time: they all wait for the
 * same promise and then retry with the new token.
 */
export function refreshAccessToken(): Promise<string | null> {
  refreshing ??= (async () => {
    try {
      const response = await raw.post<unknown>('/identity/auth/refresh', null, {
        headers: { [XSRF_HEADER]: readCookie(XSRF_COOKIE) ?? '' },
      });
      const session = sessionSchema.parse(response.data);
      useAuthStore.getState().setSession(session.accessToken);
      return session.accessToken;
    } catch {
      return null;
    } finally {
      refreshing = null;
    }
  })();
  return refreshing;
}

http.interceptors.request.use((config) => {
  const token = useAuthStore.getState().accessToken;
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  config.headers['Accept-Language'] = httpHooks.language();
  const xsrf = readCookie(XSRF_COOKIE);
  if (xsrf) {
    config.headers[XSRF_HEADER] = xsrf;
  }

  return config;
});

http.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const config = error.config as RetriableConfig | undefined;
    const isAuthEndpoint = AUTH_ENDPOINTS.some((path) => config?.url?.startsWith(path));

    if (error.response?.status === 401 && config && !config._retried && !isAuthEndpoint) {
      config._retried = true;
      const token = await refreshAccessToken();
      if (token) {
        config.headers.Authorization = `Bearer ${token}`;
        return http.request(config);
      }

      useAuthStore.getState().clear();
      httpHooks.onSessionExpired();
    }

    return Promise.reject(ApiError.from(error));
  },
);
