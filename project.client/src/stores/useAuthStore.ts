import { create } from "zustand";

import {
  api,
  readToken,
  setAuthorization,
} from "../configs/axios/interceptors";
import { authInitialState } from "../configs/constants";
import { tryParseEnvelope } from "../services/zodApi";
import type { Authorizations } from "../types/Authorizations";
import type { InitialAuth } from "../types/InitialAuth";
import { authWithRefreshResponseSchema } from "../types/schemas";
import { retrase } from "../utils/viewTransition";

interface AuthState {
  authState: InitialAuth;
  loading: boolean;
  /**
   * Boot-time hydration:
   *   1. POST /Auth/Refresh — the HttpOnly refresh cookie attaches
   *      automatically. On 200 we get a fresh access token; on 401
   *      the user must log in again.
   *
   * Never writes the token to localStorage (F1).
   */
  syncAuth: () => Promise<void>;
  signIn: (login: InitialAuth) => void;
  /**
   * Server-side revocation (POST /Auth/Logout) + clear the in-memory store.
   * Best-effort: if the server call fails we still clear locally.
   */
  logout: () => Promise<void>;
}

const apiBaseUrl = api.defaults.baseURL ?? "";

export const useAuthStore = create<AuthState>((set) => ({
  authState: authInitialState,
  loading: true,
  syncAuth: async () => {
    set({ loading: true });
    try {
      const response = await fetch(`${apiBaseUrl}Auth/Refresh`, {
        method: "POST",
        credentials: "include",
        headers: {
          ...(readXsrfCookie() ? { "X-XSRF-TOKEN": readXsrfCookie()! } : {}),
        },
      });

      if (response.ok) {
        const raw = await response.json();
        const env = tryParseEnvelope(raw, authWithRefreshResponseSchema);
        if (env?.success && env.data?.token) {
          const data = env.data;
          const auth: InitialAuth = {
            isLoggedIn: true,
            redirect: false,
            email: data.email ?? "",
            token: data.token ?? "",
            userName: data.userName ?? "",
            name: data.name ?? "",
            userId: data.userId ?? 0,
            operations: (data.operations ?? []) as unknown as Authorizations[],
          };
          setAuthorization(auth.token);
          set({ authState: auth });
        }
      } else if (response.status !== 401) {
        console.warn("refresh bootstrap failed", response.status);
      }
    } catch (error) {
      console.error("auth sync failed", error);
    } finally {
      set({ loading: false });
    }
  },
  signIn: (auth) => {
    if (auth.token) {
      setAuthorization(auth.token);
    }
    set({ authState: auth });
  },
  logout: async () => {
    set({ loading: true });
    try {
      await fetch(`${apiBaseUrl}Auth/Logout`, {
        method: "POST",
        credentials: "include",
        headers: {
          ...(readXsrfCookie() ? { "X-XSRF-TOKEN": readXsrfCookie()! } : {}),
        },
      });
    } catch (error) {
      console.error("logout request failed", error);
    } finally {
      setAuthorization(null);
      set({ authState: authInitialState, loading: false });
    }
  },
}));

// Expose the current token accessor for the request interceptor.
export const currentAuthToken = readToken;

function readXsrfCookie(): string | null {
  if (typeof document === "undefined") return null;
  const target = "XSRF-TOKEN=";
  const parts = document.cookie.split(";");
  for (const raw of parts) {
    const cookie = raw.trim();
    if (cookie.startsWith(target)) {
      return decodeURIComponent(cookie.substring(target.length));
    }
  }
  return null;
}

// `retrase` is reserved for future use in the sign-in flow; kept imported
// so the bundler doesn't drop it from the dep graph.
void retrase;
