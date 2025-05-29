import type { RouteObject } from "react-router";

import { nameRoutes } from "../configs/constants";
import LoadingPage from "../pages/public/LoadingPage";
import { TestPage } from "../pages/public/TestPage";
import ProtectedPublic from "./middlewares/ProtectedPublic";

export const PublicRoutes: RouteObject[] = [
  {
    path: nameRoutes.login,
    lazy: () => import("../pages/auth/LoginPage"),
    hydrateFallbackElement: <LoadingPage />,
  },
  {
    path: nameRoutes.changePassword,
    lazy: () => import("../pages/auth/ChangePasswordPage"),
    hydrateFallbackElement: <LoadingPage />,
  },
  {
    index: true,
    element: (
      <ProtectedPublic>
        <TestPage />
      </ProtectedPublic>
    ),
  },
];
