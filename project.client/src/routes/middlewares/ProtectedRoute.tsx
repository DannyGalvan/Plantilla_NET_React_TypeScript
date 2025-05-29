import type { ReactNode } from "react";
import { Navigate } from "react-router";

import { nameRoutes } from "../../configs/constants";
import { useAuth } from "../../hooks/useAuth";

interface ProtectedRouteProps {
  readonly children: ReactNode;
  readonly operation: string;
}

function ProtectedRoute({ children, operation }: ProtectedRouteProps) {
  const { isLoggedIn, redirect, allOperations } = useAuth();

  if (!allOperations.some((op) => op.name === operation)) {
    return <Navigate to={nameRoutes.error} />;
  }

  if (!isLoggedIn) {
    return <Navigate to={nameRoutes.login} />;
  }

  if (redirect) {
    return <Navigate to={nameRoutes.changePassword} />;
  }

  return children;
}

export default ProtectedRoute;
