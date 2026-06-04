import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { expect, it, vi } from 'vitest';
import { AuthContext, type AuthContextValue } from './auth-context';
import { AdminRoute } from './AdminRoute';

function renderForRole(role: string) {
  const value: AuthContextValue = {
    isAuthenticated: true,
    reload: vi.fn(),
    logout: vi.fn(),
    session: {
      accessToken: 'access',
      refreshToken: 'refresh',
      accessTokenExpiresAt: new Date().toISOString(),
      account: { id: 'account', email: 'a@example.com', username: 'a', role },
    },
  };

  render(
    <AuthContext.Provider value={value}>
      <MemoryRouter initialEntries={['/admin/campaigns']}>
        <Routes>
          <Route element={<AdminRoute />}>
            <Route path="/admin/campaigns" element={<div>admin area</div>} />
          </Route>
          <Route path="/games" element={<div>games area</div>} />
        </Routes>
      </MemoryRouter>
    </AuthContext.Provider>,
  );
}

it('allows admin accounts', () => {
  renderForRole('admin');
  expect(screen.getByText('admin area')).toBeInTheDocument();
});

it('redirects non-admin accounts', () => {
  renderForRole('user');
  expect(screen.getByText('games area')).toBeInTheDocument();
});
