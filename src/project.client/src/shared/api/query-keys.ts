import { useAuthStore } from '@/shared/auth/auth-store';

/**
 * Prefix of every query key: the active tenant and organization unit. Cached data of a previous context can never
 * be shown after switching (the whole cache is also cleared on switch).
 */
export function useQueryScope(...parts: string[]): readonly unknown[] {
  const tenantId = useAuthStore((state) => state.profile?.tenant?.id ?? 'anonymous');
  const unitId = useAuthStore((state) => state.profile?.organizationUnitId ?? 'all');
  return [tenantId, unitId, ...parts];
}
