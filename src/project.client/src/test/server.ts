import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';

export const api = (path: string) => `http://localhost:3000/api/v1${path}`;

export const adminProfile = {
  id: 'u-1',
  userName: 'admin',
  email: 'admin@example.com',
  displayName: 'Admin',
  mustChangePassword: false,
  tenant: { id: 't-1', code: 'default', name: 'Default', isDefault: true },
  organizationUnitId: null,
  roles: ['SuperAdmin'],
  permissions: ['catalog.products.read', 'catalog.products.create'],
  tenants: [{ id: 't-1', code: 'default', name: 'Default', isDefault: true }],
  permissionsVersion: 1,
  isPlatformAdmin: true,
};

export const session = (token: string) => ({
  accessToken: token,
  expiresAt: new Date(Date.now() + 60_000).toISOString(),
  mustChangePassword: false,
  tenantId: 't-1',
  organizationUnitId: null,
});

/** Default API mocks; tests override them with server.use(...). */
export const server = setupServer(
  http.get(api('/platform/config'), () =>
    HttpResponse.json({
      appName: 'Test',
      tenancyMode: 'None',
      tenant: null,
      branding: {
        displayName: 'Test Co',
        logoUrl: null,
        faviconUrl: null,
        themePreset: 'ocean',
        primaryColor: null,
        radius: '0.5rem',
        allowUserPrimaryOverride: true,
      },
      localization: { locale: 'en', timeZone: 'UTC', currency: 'EUR', dateFormat: 'dd/MM/yyyy' },
      features: [],
      modules: ['identity', 'catalog'],
      organizationEnabled: false,
      languages: ['es', 'en'],
    }),
  ),
  http.get(api('/me'), () => HttpResponse.json(adminProfile)),
);
