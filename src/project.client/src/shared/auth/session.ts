import { z } from 'zod';

import { http, readCookie, refreshAccessToken } from '@/shared/api/http-client';

import { useAuthStore } from './auth-store';
import { navigationItemSchema, profileSchema, sessionSchema, type Session } from './types';

/** Auth I/O. The store holds state only; these functions talk to the API. */
export const authApi = {
  async login(userName: string, password: string): Promise<Session> {
    const session = sessionSchema.parse((await http.post('/identity/auth/login', { userName, password })).data);
    useAuthStore.getState().setSession(session.accessToken);
    return session;
  },

  async logout(): Promise<void> {
    try {
      await http.post('/identity/auth/logout');
    } finally {
      useAuthStore.getState().clear();
    }
  },

  async loadProfile() {
    const profile = profileSchema.parse((await http.get('/me')).data);
    useAuthStore.getState().setProfile(profile);
    return profile;
  },

  async navigation() {
    return z.array(navigationItemSchema).parse((await http.get('/me/navigation')).data);
  },

  async changePassword(currentPassword: string, newPassword: string) {
    const session = sessionSchema.parse(
      (await http.post('/identity/auth/change-password', { currentPassword, newPassword })).data,
    );
    useAuthStore.getState().setSession(session.accessToken);
    return session;
  },

  async forgotPassword(email: string) {
    await http.post('/identity/auth/forgot-password', { email });
  },

  async resetPassword(token: string, newPassword: string) {
    await http.post('/identity/auth/reset-password', { token, newPassword });
  },

  async switchContext(tenantId: string, organizationUnitId: string | null) {
    const session = sessionSchema.parse(
      (await http.post('/identity/auth/switch-context', { tenantId, organizationUnitId })).data,
    );
    useAuthStore.getState().setSession(session.accessToken);
    return session;
  },
};

/**
 * Restores the session at startup with the refresh cookie (if any), then loads the profile.
 * Ends as authenticated or anonymous; never throws.
 */
export async function restoreSession(): Promise<void> {
  const store = useAuthStore.getState();
  if (!readCookie('XSRF-TOKEN')) {
    store.clear();
    return;
  }

  const token = await refreshAccessToken();
  if (!token) {
    store.clear();
    return;
  }

  try {
    await authApi.loadProfile();
  } catch {
    store.clear();
  }
}
