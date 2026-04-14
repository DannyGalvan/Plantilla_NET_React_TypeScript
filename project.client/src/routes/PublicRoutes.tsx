import type { RouteObject } from "react-router";

import { nameRoutes } from "../configs/constants";
import LoadingPage from "../pages/public/LoadingPage";
import { TestPage } from "../pages/public/TestPage";
import ProtectedPublic from "./middlewares/ProtectedPublic";

// Import pages
import { CreateRolPage } from "../pages/rol/CreateRolPage";
import { RolPage } from "../pages/rol/RolPage";
import { UpdateRolPage } from "../pages/rol/UpdateRolPage";
import { CreateUserPage } from "../pages/user/CreateUserPage";
import { UpdateUserPage } from "../pages/user/UpdateUserPage";
import { UserPage } from "../pages/user/UserPage";

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
  {
    path: nameRoutes.user,
    element: (
      <ProtectedPublic>
        <UserPage />
      </ProtectedPublic>
    ),
  },
  {
    path: nameRoutes.userCreate,
    element: (
      <ProtectedPublic>
        <CreateUserPage />
      </ProtectedPublic>
    ),
  },
  {
    path: `${nameRoutes.userUpdate}/:id`,
    element: (
      <ProtectedPublic>
        <UpdateUserPage />
      </ProtectedPublic>
    ),
  },
  {
    path: nameRoutes.rol,
    element: (
      <ProtectedPublic>
        <RolPage />
      </ProtectedPublic>
    ),
  },
  {
    path: nameRoutes.rolCreate,
    element: (
      <ProtectedPublic>
        <CreateRolPage />
      </ProtectedPublic>
    ),
  },
  {
    path: `${nameRoutes.rolUpdate}/:id`,
    element: (
      <ProtectedPublic>
        <UpdateRolPage />
      </ProtectedPublic>
    ),
  },
];
