import { create } from 'zustand';

import type { Profile } from './types';

export type AuthStatus = 'unknown' | 'authenticated' | 'anonymous';

interface AuthState {
  status: AuthStatus;
  accessToken: string | null;
  profile: Profile | null;
  permissions: ReadonlySet<string>;
  setSession: (accessToken: string) => void;
  setProfile: (profile: Profile) => void;
  clear: () => void;
}

/**
 * Session state only (no I/O). The access token lives in memory; the refresh token is an HttpOnly cookie the
 * JavaScript code never sees.
 */
export const useAuthStore = create<AuthState>((set) => ({
  status: 'unknown',
  accessToken: null,
  profile: null,
  permissions: new Set<string>(),
  setSession: (accessToken) => set({ accessToken }),
  setProfile: (profile) => set({ profile, permissions: new Set(profile.permissions), status: 'authenticated' }),
  clear: () => set({ status: 'anonymous', accessToken: null, profile: null, permissions: new Set<string>() }),
}));
