import { RouterProvider } from "react-router";

import { useAuthorizationRoutes } from "../hooks/useAuthorizationRoutes";

function AppRoutes() {
  const routes = useAuthorizationRoutes();

  return <RouterProvider router={routes} />;
}

export default AppRoutes;
