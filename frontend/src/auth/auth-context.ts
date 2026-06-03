import { createContext } from 'react';
import type { AuthSession } from '../shared/api/auth-storage';

export interface AuthContextValue {
  session: AuthSession | null;
  isAuthenticated: boolean;
  reload: () => void;
  logout: () => Promise<void>;
}

export const AuthContext = createContext<AuthContextValue | null>(null);
