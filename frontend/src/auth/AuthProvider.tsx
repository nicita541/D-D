import { useEffect, useMemo, useState, type ReactNode } from 'react';
import { clearAuthSession, readAuthSession, type AuthSession } from '../shared/api/auth-storage';
import { authApi } from '../shared/api/endpoints';
import { AuthContext, type AuthContextValue } from './auth-context';

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<AuthSession | null>(() => readAuthSession());

  const reload = () => setSession(readAuthSession());

  useEffect(() => {
    const handler = () => reload();
    window.addEventListener('dnd-auth-changed', handler);
    window.addEventListener('storage', handler);
    return () => {
      window.removeEventListener('dnd-auth-changed', handler);
      window.removeEventListener('storage', handler);
    };
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      session,
      isAuthenticated: Boolean(session?.accessToken),
      reload,
      async logout() {
        const refreshToken = readAuthSession()?.refreshToken;
        if (refreshToken) {
          try {
            await authApi.logout(refreshToken);
          } catch {
            // Logout must clear local state even if the network is already gone.
          }
        }

        clearAuthSession();
      },
    }),
    [session],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
