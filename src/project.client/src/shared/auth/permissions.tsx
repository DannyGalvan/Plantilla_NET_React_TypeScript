import type { ReactNode } from 'react';

import { useAuthStore } from './auth-store';

/** True when the signed-in user has the permission (`{module}.{resource}.{action}`). */
export function usePermission(permission: string | undefined): boolean {
  return useAuthStore((state) => !permission || state.permissions.has(permission));
}

/** Renders its children only when the user has the permission. */
export function Can({
  permission,
  children,
  fallback = null,
}: {
  permission: string;
  children: ReactNode;
  fallback?: ReactNode;
}) {
  return usePermission(permission) ? children : fallback;
}
