import { clearAuthSession, readAuthSession, saveAuthSession } from './auth-storage';
import { ApiError, type ApiErrorPayload, type AuthResponse } from './types';

const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL ?? '/api').replace(/\/$/, '');

type RequestBody = BodyInit | Record<string, unknown> | unknown[] | null | undefined;

interface ApiRequestOptions extends Omit<RequestInit, 'body'> {
  body?: RequestBody;
  skipAuth?: boolean;
  retryingAfterRefresh?: boolean;
}

export function apiBaseUrl() {
  return API_BASE_URL;
}

export async function apiGet<T>(path: string, options: ApiRequestOptions = {}) {
  return apiRequest<T>(path, { ...options, method: 'GET' });
}

export async function apiPost<T>(path: string, body?: RequestBody, options: ApiRequestOptions = {}) {
  return apiRequest<T>(path, { ...options, method: 'POST', body });
}

export async function apiPut<T>(path: string, body?: RequestBody, options: ApiRequestOptions = {}) {
  return apiRequest<T>(path, { ...options, method: 'PUT', body });
}

export async function apiDelete<T>(path: string, options: ApiRequestOptions = {}) {
  return apiRequest<T>(path, { ...options, method: 'DELETE' });
}

async function apiRequest<T>(path: string, options: ApiRequestOptions): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`, buildRequest(options));

  if (response.status === 401 && !options.skipAuth && !options.retryingAfterRefresh) {
    const refreshed = await refreshTokens();
    if (refreshed) {
      return apiRequest<T>(path, { ...options, retryingAfterRefresh: true });
    }
  }

  if (!response.ok) {
    throw await toApiError(response);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

function buildRequest(options: ApiRequestOptions): RequestInit {
  const headers = new Headers(options.headers);
  const session = readAuthSession();

  if (!options.skipAuth && session?.accessToken) {
    headers.set('Authorization', `Bearer ${session.accessToken}`);
  }

  let body = options.body as BodyInit | undefined;
  if (options.body && !(options.body instanceof FormData) && typeof options.body !== 'string') {
    headers.set('Content-Type', 'application/json');
    body = JSON.stringify(options.body);
  }

  return {
    ...options,
    headers,
    body,
  };
}

async function refreshTokens() {
  const session = readAuthSession();
  if (!session?.refreshToken) {
    clearAuthSession();
    return false;
  }

  try {
    const response = await fetch(`${API_BASE_URL}/auth/refresh`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken: session.refreshToken }),
    });

    if (!response.ok) {
      clearAuthSession();
      return false;
    }

    saveAuthSession((await response.json()) as AuthResponse);
    return true;
  } catch {
    clearAuthSession();
    return false;
  }
}

async function toApiError(response: Response) {
  let payload: ApiErrorPayload | string | undefined;
  try {
    payload = (await response.json()) as ApiErrorPayload;
  } catch {
    payload = await response.text();
  }

  const message =
    typeof payload === 'string'
      ? payload
      : payload?.message ?? payload?.error ?? payload?.detail ?? payload?.title ?? defaultErrorMessage(response.status);

  return new ApiError(response.status, message, payload);
}

function defaultErrorMessage(status: number) {
  if (status === 400) return 'Некорректный запрос.';
  if (status === 401) return 'Нужно войти в аккаунт.';
  if (status === 403) return 'Недостаточно прав.';
  if (status === 404) return 'Данные не найдены.';
  if (status === 409) return 'Конфликт состояния.';
  if (status === 503) return 'Сервис временно недоступен.';
  return 'Backend вернул ошибку.';
}
