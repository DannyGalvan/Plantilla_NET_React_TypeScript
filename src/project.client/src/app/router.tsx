import { createBrowserRouter, type RouteObject } from 'react-router';

import { RequireAuth } from '@/shared/auth/guards';
import { AppShell } from '@/shared/components/app-shell';
import { NotFoundPage, RouteErrorPage } from '@/shared/components/errors';
import type { ModuleManifest } from '@/shared/modules/manifest';

/** Builds the route tree once, from the active module manifests. */
export function createRoutes(modules: ModuleManifest[]): RouteObject[] {
  return [
    {
      errorElement: <RouteErrorPage />,
      children: [
        ...modules.flatMap((module) => module.publicRoutes ?? []),
        {
          element: (
            <RequireAuth>
              <AppShell />
            </RequireAuth>
          ),
          errorElement: <RouteErrorPage />,
          children: [...modules.flatMap((module) => module.routes ?? []), { path: '*', element: <NotFoundPage /> }],
        },
      ],
    },
  ];
}

export function createAppRouter(modules: ModuleManifest[]) {
  return createBrowserRouter(createRoutes(modules));
}
