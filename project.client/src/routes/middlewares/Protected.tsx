import { Navigate } from "react-router";

import { nameRoutes } from "../../configs/constants";
import { useAuth } from "../../hooks/useAuth";
import { useErrorsStore } from "../../stores/useErrorsStore";

interface ProtectedProps {
  readonly children: React.ReactNode;
}

function Protected({ children }: ProtectedProps) {
  const { isLoggedIn, redirect } = useAuth();
  const { error } = useErrorsStore();

  if (!isLoggedIn) {
    return <Navigate to={nameRoutes.login} />;
  }

  if (error) {
    return <Navigate to={nameRoutes.error} />;
  }

  if (redirect) {
    return <Navigate to={nameRoutes.changePassword} />;
  }

  return children;
}

export default Protected;
