import { render, screen } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { beforeEach, describe, expect, it } from 'vitest';

import { adminProfile } from '@/test/server';

import { useAuthStore } from './auth-store';
import { RequireAuth, RequirePermission } from './guards';
import { Can } from './permissions';

function renderAt(path: string, element: React.ReactNode) {
  const router = createMemoryRouter(
    [
      { path: '/login', element: <p>login page</p> },
      { path: '/change-password', element: <p>change password</p> },
      { path: '*', element },
    ],
    { initialEntries: [path] },
  );
  return render(<RouterProvider router={router} />);
}

describe('auth guards', () => {
  beforeEach(() => useAuthStore.getState().clear());

  it('redirects anonymous users to the login page', () => {
    renderAt(
      '/catalog',
      <RequireAuth>
        <p>private</p>
      </RequireAuth>,
    );
    expect(screen.getByText('login page')).toBeInTheDocument();
  });

  it('forces the password change before anything else', () => {
    useAuthStore.getState().setProfile({ ...adminProfile, mustChangePassword: true });
    renderAt(
      '/catalog',
      <RequireAuth>
        <p>private</p>
      </RequireAuth>,
    );
    expect(screen.getByText('change password')).toBeInTheDocument();
  });

  it('renders protected content for signed-in users', () => {
    useAuthStore.getState().setProfile(adminProfile);
    renderAt(
      '/catalog',
      <RequireAuth>
        <p>private</p>
      </RequireAuth>,
    );
    expect(screen.getByText('private')).toBeInTheDocument();
  });

  it('shows the 403 page without the permission and hides actions with <Can>', () => {
    useAuthStore.getState().setProfile(adminProfile);
    renderAt(
      '/users',
      <>
        <RequirePermission permission="identity.users.read">
          <p>users</p>
        </RequirePermission>
        <Can permission="catalog.products.create">
          <button type="button">create product</button>
        </Can>
        <Can permission="catalog.products.delete">
          <button type="button">delete product</button>
        </Can>
      </>,
    );

    expect(screen.queryByText('users')).not.toBeInTheDocument();
    expect(screen.getByText('403')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'create product' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'delete product' })).not.toBeInTheDocument();
  });
});
