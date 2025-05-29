import { Navigate } from "react-router";

import { nameRoutes } from "../../configs/constants";
import { useAuth } from "../../hooks/useAuth";

interface ProtectedProps {
  readonly children: React.ReactNode;
}

function ProtectedError({ children }: ProtectedProps) {
  const { isLoggedIn, redirect } = useAuth();

  if (!isLoggedIn) {
    return <Navigate to={nameRoutes.login} />;
  }

  if (redirect) {
    return <Navigate to={nameRoutes.changePassword} />;
  }

  return children;
}

export default ProtectedError;
