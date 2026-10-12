import { RequireGuest } from '@/shared/auth/guards';
import type { ModuleManifest } from '@/shared/modules/manifest';

import { identityResources } from './i18n';
import { ChangePasswordPage, ForgotPasswordPage, LoginPage, ResetPasswordPage } from './pages/AuthPages';

const manifest: ModuleManifest = {
  id: 'identity',
  backendModule: 'identity',
  i18n: identityResources,
  publicRoutes: [
    {
      path: '/login',
      element: (
        <RequireGuest>
          <LoginPage />
        </RequireGuest>
      ),
    },
    { path: '/forgot-password', element: <ForgotPasswordPage /> },
    { path: '/reset-password', element: <ResetPasswordPage /> },
  ],
  routes: [
    { path: 'change-password', element: <ChangePasswordPage />, handle: { titleKey: 'identity:changePassword.title' } },
    {
      path: 'profile',
      lazy: async () => ({ Component: (await import('./pages/ProfilePage')).ProfilePage }),
      handle: { titleKey: 'nav.profile' },
    },
    {
      path: 'identity/users',
      lazy: async () => ({ Component: (await import('./pages/UsersPage')).UsersPage }),
      handle: { permission: 'identity.users.read', titleKey: 'nav.users' },
    },
    {
      path: 'identity/roles',
      lazy: async () => ({ Component: (await import('./pages/RolesPage')).RolesPage }),
      handle: { permission: 'identity.roles.read', titleKey: 'nav.roles' },
    },
  ],
};

export default manifest;
