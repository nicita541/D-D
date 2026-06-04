import { saveAuthSession } from '../auth-storage';
import { apiGet, apiPost } from '../client';
import type { AuthResponse } from '../types';

export const authApi = {
  async register(body: { email: string; username: string; password: string; displayName?: string }) {
    return saveAuthSession(await apiPost<AuthResponse>('/auth/register', body, { skipAuth: true }));
  },
  async login(body: { emailOrUsername: string; password: string }) {
    return saveAuthSession(await apiPost<AuthResponse>('/auth/login', body, { skipAuth: true }));
  },
  me: () => apiGet<AuthResponse['account']>('/auth/me'),
  logout: (refreshToken: string) => apiPost('/auth/logout', { refreshToken }),
};
