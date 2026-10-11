import { useAuthStore } from './auth-store';

export function usePermission(key: string): boolean {
  return useAuthStore((state) => state.permissions.has(key));
}

export function useAnyPermission(keys: ReadonlyArray<string>): boolean {
  return useAuthStore((state) => keys.some((k) => state.permissions.has(k)));
}
