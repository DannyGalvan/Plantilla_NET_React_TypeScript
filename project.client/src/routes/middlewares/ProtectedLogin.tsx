import { Navigate } from "react-router";

import { nameRoutes } from "../../configs/constants";
import { useAuth } from "../../hooks/useAuth";

interface ProtectedLoginProps {
  readonly children: React.ReactNode;
}

function ProtectedLogin({ children }: ProtectedLoginProps) {
  const { isLoggedIn } = useAuth();

  if (isLoggedIn) {
    return <Navigate to={nameRoutes.root} />;
  }

  return children;
}

export default ProtectedLogin;
