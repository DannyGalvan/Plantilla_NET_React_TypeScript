import type { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router';

import { ForbiddenPage } from '@/shared/components/errors';
import { Spinner } from '@/shared/components/page';

import { useAuthStore } from './auth-store';
import { usePermission } from './permissions';

/** Authenticated area. Users who must change their password are sent to that page first. */
export function RequireAuth({ children }: { children: ReactNode }) {
  const status = useAuthStore((state) => state.status);
  const mustChange = useAuthStore((state) => state.profile?.mustChangePassword ?? false);
  const location = useLocation();

  if (status === 'unknown') {
    return <Spinner className="min-h-screen" />;
  }

  if (status === 'anonymous') {
    return <Navigate to={`/login?returnTo=${encodeURIComponent(location.pathname + location.search)}`} replace />;
  }

  if (mustChange && location.pathname !== '/change-password') {
    return <Navigate to="/change-password" replace />;
  }

  return children;
}

/** Pages for anonymous users only (sign in): signed-in users go home. */
export function RequireGuest({ children }: { children: ReactNode }) {
  const status = useAuthStore((state) => state.status);
  if (status === 'unknown') {
    return <Spinner className="min-h-screen" />;
  }

  return status === 'authenticated' ? <Navigate to="/" replace /> : children;
}

/** Shows the 403 page when the permission is missing (the API enforces it too; this only improves the UX). */
export function RequirePermission({ permission, children }: { permission?: string; children: ReactNode }) {
  return usePermission(permission) ? children : <ForbiddenPage />;
}
