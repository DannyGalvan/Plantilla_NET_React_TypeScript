import { z } from 'zod';

import {
  createHttpClient,
  setAccessToken,
  type ApiError,
} from '../../../shared/api/http-client';
import type { AuthSession } from '../../../shared/auth/types';
import { env } from '../../../shared/config/env';

export const loginSchema = z.object({
  userName: z.string().min(1, 'Requerido'),
  password: z.string().min(1, 'Requerido'),
});

export const changePasswordSchema = z
  .object({
    currentPassword: z.string().min(1, 'Requerido'),
    newPassword: z.string().min(8, 'Mínimo 8 caracteres'),
    confirmPassword: z.string().min(1, 'Requerido'),
  })
  .refine((data) => data.newPassword === data.confirmPassword, {
    message: 'No coincide',
    path: ['confirmPassword'],
  });

export type LoginInput = z.infer<typeof loginSchema>;
export type ChangePasswordInput = z.infer<typeof changePasswordSchema>;

const http = createHttpClient({
  baseURL: env.VITE_API_URL,
  onUnauthorized: () => {
    setAccessToken(null);
    window.dispatchEvent(new CustomEvent('auth:logout'));
  },
  refresh: async () => {
    try {
      const res = await fetch(`${env.VITE_API_URL}/identity/auth/refresh`, {
        method: 'POST',
        credentials: 'include',
      });
      if (!res.ok) return null;
      const body = (await res.json()) as { accessToken?: string };
      return body.accessToken ?? null;
    } catch {
      return null;
    }
  },
});

interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: AuthSession['user'];
  permissions: string[];
}

export async function login(input: LoginInput): Promise<AuthSession> {
  const { data } = await http.post<LoginResponse>(
    '/identity/auth/login',
    input,
  );
  setAccessToken(data.accessToken);
  return {
    user: data.user,
    permissions: data.permissions,
    expiresAt: data.expiresAt,
  };
}

export async function changePassword(
  input: ChangePasswordInput,
): Promise<void> {
  await http.post('/identity/auth/change-password', {
    currentPassword: input.currentPassword,
    newPassword: input.newPassword,
  });
}

export async function getMyProfile(): Promise<AuthSession['user']> {
  const { data } = await http.get<AuthSession['user']>('/identity/me');
  return data as AuthSession['user'];
}

export function parseApiError(error: unknown): {
  code?: string;
  detail?: string;
  errors?: Record<string, string[]>;
} {
  const apiError = error as ApiError & {
    problem?: {
      code?: string;
      detail?: string;
      errors?: Record<string, string[]>;
    };
  };
  return apiError.problem ?? {};
}
