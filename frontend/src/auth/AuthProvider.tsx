import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { clearAuthSession, readAuthSession, type AuthSession } from '../shared/api/auth-storage';
import { authApi } from '../shared/api/endpoints';
import { AuthContext, type AuthContextValue } from './auth-context';

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const [session, setSession] = useState<AuthSession | null>(() => readAuthSession());
  const accountId = useRef(session?.account.id);

  const reload = useCallback(() => {
    const next = readAuthSession();
    if (accountId.current !== next?.account.id) {
      queryClient.clear();
      accountId.current = next?.account.id;
    }
    setSession(next);
  }, [queryClient]);

  useEffect(() => {
    const handler = () => reload();
    window.addEventListener('dnd-auth-changed', handler);
    window.addEventListener('storage', handler);
    return () => {
      window.removeEventListener('dnd-auth-changed', handler);
      window.removeEventListener('storage', handler);
    };
  }, [reload]);

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
    [reload, session],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
