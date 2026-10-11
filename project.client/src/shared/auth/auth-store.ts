import { create } from 'zustand';

import type { AuthSession, AuthUser } from './types';

interface AuthState {
  status: 'idle' | 'authenticated' | 'unauthenticated';
  user: AuthUser | null;
  permissions: Set<string>;
  setSession(session: AuthSession): void;
  setPermissions(perms: ReadonlyArray<string>): void;
  clear(): void;
}

/**
 * In-memory auth store. The access token lives outside the store (in
 * \`http-client.ts\`); the store carries only the data the UI needs.
 *
 * The bootstrap routine in \`shared/auth/session.ts\` runs the refresh-on-load
 * and dispatches \`setSession\`/\`clear\`.
 */
export const useAuthStore = create<AuthState>((set) => ({
  status: 'idle',
  user: null,
  permissions: new Set<string>(),

  setSession(session) {
    set({
      status: 'authenticated',
      user: session.user,
      permissions: new Set(session.permissions),
    });
  },

  setPermissions(perms) {
    set({ permissions: new Set(perms) });
  },

  clear() {
    set({
      status: 'unauthenticated',
      user: null,
      permissions: new Set<string>(),
    });
  },
}));

export function selectIsAuthenticated(state: AuthState): boolean {
  return state.status === 'authenticated' && state.user !== null;
}

export function selectHasPermission(state: AuthState, key: string): boolean {
  return state.permissions.has(key);
}
