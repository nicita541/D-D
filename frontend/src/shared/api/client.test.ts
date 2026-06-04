import { afterEach, expect, it, vi } from 'vitest';
import { saveAuthSession } from './auth-storage';
import { apiGet } from './client';

afterEach(() => {
  window.localStorage.clear();
  vi.unstubAllGlobals();
});

it('uses one refresh request for concurrent 401 responses', async () => {
  saveAuthSession({
    accessToken: 'expired',
    refreshToken: 'refresh',
    accessTokenExpiresAt: new Date().toISOString(),
    account: { id: 'account', email: 'a@example.com', username: 'a', role: 'user' },
  });

  let refreshed = false;
  let refreshCalls = 0;
  vi.stubGlobal('fetch', vi.fn(async (input: RequestInfo | URL) => {
    const url = String(input);
    if (url.endsWith('/auth/refresh')) {
      refreshCalls += 1;
      await new Promise((resolve) => setTimeout(resolve, 5));
      refreshed = true;
      return Response.json({
        accessToken: 'fresh',
        refreshToken: 'refresh-2',
        accessTokenExpiresAt: new Date().toISOString(),
        account: { id: 'account', email: 'a@example.com', username: 'a', role: 'user' },
      });
    }
    return refreshed ? Response.json({ ok: true }) : new Response('', { status: 401 });
  }));

  await Promise.all([apiGet('/one'), apiGet('/two')]);
  expect(refreshCalls).toBe(1);
});
