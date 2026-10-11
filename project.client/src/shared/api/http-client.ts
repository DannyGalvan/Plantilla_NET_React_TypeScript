import type { AxiosInstance, AxiosRequestConfig } from 'axios';
import axios, { AxiosError } from 'axios';

import type { ProblemDetails } from './problem-details';

/**
 * HTTP client with single-flight refresh-on-401.
 *
 * - Access token in memory (never localStorage).
 * - Refresh token in HttpOnly cookie (set by the backend).
 * - Concurrent 401s share a single refresh call.
 * - On refresh failure we dispatch a \`auth:logout\` event so the auth store
 *   can clear state and redirect to /login.
 */
export interface ApiError extends AxiosError {
  problem?: ProblemDetails;
}

export interface HttpClientOptions {
  baseURL?: string;
  onUnauthorized: () => void;
  refresh: () => Promise<string | null>;
}

export function createHttpClient(opts: HttpClientOptions): AxiosInstance {
  const instance = axios.create({
    baseURL: opts.baseURL ?? import.meta.env.VITE_API_URL ?? '',
    withCredentials: true,
  });

  let inflightRefresh: Promise<string | null> | null = null;

  instance.interceptors.request.use((config) => {
    const token = readAccessToken();
    if (token) {
      config.headers.set('Authorization', `Bearer ${token}`);
    }
    return config;
  });

  instance.interceptors.response.use(
    (response) => response,
    async (error: ApiError) => {
      if (error.response?.status !== 401) return Promise.reject(error);

      try {
        inflightRefresh ??= opts.refresh().finally(() => {
          inflightRefresh = null;
        });
        const newToken = await inflightRefresh;
        if (!newToken) {
          opts.onUnauthorized();
          return Promise.reject(error);
        }

        setAccessToken(newToken);
        const retryConfig: AxiosRequestConfig = error.config ?? {};
        retryConfig.headers = {
          ...(retryConfig.headers ?? {}),
          Authorization: `Bearer ${newToken}`,
        };
        return instance.request(retryConfig);
      } catch (refreshError) {
        opts.onUnauthorized();
        return Promise.reject(refreshError);
      }
    },
  );

  return instance;
}

let accessToken: string | null = null;

export function setAccessToken(token: string | null): void {
  accessToken = token;
}

export function readAccessToken(): string | null {
  return accessToken;
}

/**
 * Parses a ProblemDetails body into a structured error. The backend uses
 * RFC 9457 with a custom \`code\` and an \`errors\` dictionary.
 */
export function parseProblem(error: AxiosError): ProblemDetails | undefined {
  const data = error.response?.data;
  if (!data || typeof data !== 'object') return undefined;
  const problem = data as ProblemDetails;
  if (typeof problem.status !== 'number') return undefined;
  return problem;
}
