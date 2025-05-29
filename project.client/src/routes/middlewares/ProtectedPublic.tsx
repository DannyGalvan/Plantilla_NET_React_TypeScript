import type { ReactNode } from "react";
import { Navigate } from "react-router";

import { nameRoutes } from "../../configs/constants";
import { useAuth } from "../../hooks/useAuth";
import { useErrorsStore } from "../../stores/useErrorsStore";

interface ProtectedPublicProps {
  readonly children: ReactNode;
}

function ProtectedPublic({ children }: ProtectedPublicProps) {
  const { isLoggedIn } = useAuth();
  const { error } = useErrorsStore();

  if (!isLoggedIn) {
    return <Navigate to={nameRoutes.login} />;
  }

  if (error) {
    return <Navigate to={nameRoutes.error} />;
  }

  return children;
}

export default ProtectedPublic;
