import { render, screen, waitFor } from '@testing-library/react';
import { userEvent } from '@testing-library/user-event';
import { QueryClientProvider } from '@tanstack/react-query';
import { http as mock, HttpResponse } from 'msw';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { beforeEach, describe, expect, it } from 'vitest';

import { useAuthStore } from '@/shared/auth/auth-store';
import { formatCurrency, setFormatSettings } from '@/shared/lib/format';
import { defaultPlatformConfig } from '@/shared/platform/platform-config';
import { ThemeProvider } from '@/shared/theme/theme-provider';
import { api, server, session } from '@/test/server';

import { activeModules, allModules } from './module-registry';
import { createQueryClient } from './query-client';
import { createRoutes } from './router';

describe('module registry', () => {
  it('discovers every module folder', () => {
    expect(allModules().map((module) => module.id)).toEqual(
      expect.arrayContaining(['identity', 'catalog', 'dashboard', 'organization', 'tenancy']),
    );
  });

  it('skips modules the backend does not load', () => {
    const ids = activeModules({ ...defaultPlatformConfig, modules: ['identity', 'tenancy'] }).map(
      (module) => module.id,
    );
    expect(ids).toContain('identity');
    expect(ids).toContain('dashboard');
    expect(ids).not.toContain('catalog');
    expect(ids).not.toContain('organization');
  });
});

describe('formatting', () => {
  it('uses the configured locale and currency', () => {
    setFormatSettings({ locale: 'en-US', currency: 'EUR' });
    expect(formatCurrency(1234.5)).toBe('€1,234.50');
  });
});

describe('sign in', () => {
  beforeEach(() => useAuthStore.getState().clear());

  it('signs in, loads the profile and opens the dashboard', async () => {
    server.use(
      mock.post(api('/identity/auth/login'), () => HttpResponse.json(session('token'))),
      mock.get(api('/me/navigation'), () =>
        HttpResponse.json([
          {
            key: 'catalog.products',
            module: 'catalog',
            labelKey: 'nav.products',
            path: '/catalog/products',
            icon: 'package',
            order: 1,
          },
        ]),
      ),
      mock.get(api('/catalog/products'), () =>
        HttpResponse.json({ items: [], page: 1, pageSize: 1, total: 7, totalPages: 7 }),
      ),
      mock.get(api('/catalog/categories'), () =>
        HttpResponse.json({ items: [], page: 1, pageSize: 1, total: 2, totalPages: 2 }),
      ),
    );

    const router = createMemoryRouter(createRoutes(allModules()), { initialEntries: ['/login'] });
    render(
      <QueryClientProvider client={createQueryClient()}>
        <ThemeProvider>
          <RouterProvider router={router} />
        </ThemeProvider>
      </QueryClientProvider>,
    );

    await userEvent.type(await screen.findByLabelText('User name or email'), 'admin');
    await userEvent.type(screen.getByLabelText('Password'), 'Secret.Pass-2026');
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }));

    await waitFor(() => expect(router.state.location.pathname).toBe('/'));
    expect(await screen.findByRole('link', { name: 'Products' })).toBeInTheDocument();
    expect(useAuthStore.getState().profile?.userName).toBe('admin');
  });

  it('shows the server message when the credentials are wrong', async () => {
    server.use(
      mock.post(api('/identity/auth/login'), () =>
        HttpResponse.json(
          { status: 401, code: 'auth.invalid_credentials', detail: 'The user name or password is incorrect.' },
          { status: 401 },
        ),
      ),
    );

    const router = createMemoryRouter(createRoutes(allModules()), { initialEntries: ['/login'] });
    render(
      <QueryClientProvider client={createQueryClient()}>
        <ThemeProvider>
          <RouterProvider router={router} />
        </ThemeProvider>
      </QueryClientProvider>,
    );

    await userEvent.type(await screen.findByLabelText('User name or email'), 'admin');
    await userEvent.type(screen.getByLabelText('Password'), 'wrong');
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('The user name or password is incorrect.');
  });
});
