import { useMemo } from "react";
import { createBrowserRouter, type RouteObject } from "react-router";

import { nameRoutes } from "../configs/constants";
import { Root } from "../containers/Root";
import { ErrorRoutes } from "../routes/ErrorRoutes";
import { PublicRoutes } from "../routes/PublicRoutes";

import { useAuth } from "./useAuth";

/**
 * Phase 4 F2 fix: filter the route tree that actually holds children, and
 * do it without mutating the result of `createBrowserRouter` (which is
 * frozen after construction).
 *
 * Routes without an `operation` requirement (login, change-password, etc.)
 * pass through untouched. Routes that require an operation are kept only
 * when the user has the matching `OperationKey` (matched on the route's
 * `handle.operationKey`).
 */
export const useAuthorizationRoutes = () => {
  const { allOperations } = useAuth();

  const allowedKeys = useMemo(
    () =>
      new Set(
        (allOperations ?? [])
          .map((op) => (op.name ?? "").toString())
          .filter((k) => k.length > 0),
      ),
    [allOperations],
  );

  const filteredChildren = useMemo<RouteObject[]>(() => {
    return PublicRoutes.filter((route) => {
      const required = (route.handle as { operationKey?: string } | undefined)
        ?.operationKey;
      if (!required) return true; // anonymous-friendly
      return allowedKeys.has(required);
    });
  }, [allowedKeys]);

  return createBrowserRouter([
    {
      path: nameRoutes.root,
      element: <Root />,
      children: [...filteredChildren, ...ErrorRoutes],
    },
  ]);
};
