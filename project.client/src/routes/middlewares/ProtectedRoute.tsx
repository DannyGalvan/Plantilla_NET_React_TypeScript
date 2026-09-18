import type { ReactNode } from "react";
import { Navigate } from "react-router";

import { nameRoutes } from "../../configs/constants";
import { useAuth } from "../../hooks/useAuth";

interface ProtectedRouteProps {
  readonly children: ReactNode;
  readonly operation: string;
}

/**
 * F3 fix: the previous version checked the operation before checking
 * authentication — an unauthenticated user hitting a /forbidden-style
 * route would be silently re-routed to /403 instead of /login. The new
 * order is "auth first, then permission", matching industry convention.
 */
function ProtectedRoute({ children, operation }: ProtectedRouteProps) {
  const { isLoggedIn, redirect, allOperations } = useAuth();

  if (!isLoggedIn) {
    return <Navigate to={nameRoutes.login} />;
  }

  if (redirect) {
    return <Navigate to={nameRoutes.changePassword} />;
  }

  if (!allOperations.some((op) => (op.name ?? "").toString() === operation)) {
    return <Navigate to={nameRoutes.error} />;
  }

  return children;
}

export default ProtectedRoute;
