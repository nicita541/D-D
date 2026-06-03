import type { AccountDto, AuthResponse } from './types';

const STORAGE_KEY = 'dnd.auth.v1';

export interface AuthSession {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  account: AccountDto;
}

export function readAuthSession(): AuthSession | null {
  const raw = window.localStorage.getItem(STORAGE_KEY);
  if (!raw) {
    return null;
  }

  try {
    return JSON.parse(raw) as AuthSession;
  } catch {
    window.localStorage.removeItem(STORAGE_KEY);
    return null;
  }
}

export function saveAuthSession(response: AuthResponse): AuthSession {
  const session: AuthSession = {
    accessToken: response.accessToken,
    refreshToken: response.refreshToken,
    accessTokenExpiresAt: response.accessTokenExpiresAt,
    account: response.account,
  };

  window.localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
  window.dispatchEvent(new CustomEvent('dnd-auth-changed'));
  return session;
}

export function clearAuthSession() {
  window.localStorage.removeItem(STORAGE_KEY);
  window.dispatchEvent(new CustomEvent('dnd-auth-changed'));
}
