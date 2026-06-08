import { afterEach, expect, it, vi } from 'vitest';
import { saveAuthSession } from './auth-storage';
import { invitesApi } from './domains/invites';
import { partyApi } from './domains/party';
import { snapshotsApi } from './domains/snapshots';

afterEach(() => {
  window.localStorage.clear();
  vi.unstubAllGlobals();
});

it('previews invites without auth and accepts them with auth', async () => {
  saveAuthSession({
    accessToken: 'token',
    refreshToken: 'refresh',
    accessTokenExpiresAt: new Date().toISOString(),
    account: { id: 'account', email: 'a@example.com', username: 'a', role: 'user' },
  });
  const calls: Array<{ url: string; init?: RequestInit }> = [];
  vi.stubGlobal('fetch', vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    calls.push({ url: String(input), init });
    return Response.json({ gameStateId: 'game-1' });
  }));

  await invitesApi.preview('invite token');
  await invitesApi.accept('invite token');

  expect(calls[0].url).toBe('/api/invites/invite%20token');
  expect(new Headers(calls[0].init?.headers).has('Authorization')).toBe(false);
  expect(calls[1].url).toBe('/api/invites/invite%20token/accept');
  expect(calls[1].init?.method).toBe('POST');
  expect(new Headers(calls[1].init?.headers).get('Authorization')).toBe('Bearer token');
});

it('uses dedicated party and snapshot endpoints', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = [];
  vi.stubGlobal('fetch', vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    calls.push({ url: String(input), init });
    return Response.json({});
  }));

  await partyApi.assignMyCharacter('game-1', 'char-1');
  await snapshotsApi.create('game-1', 'before boss');
  await snapshotsApi.restore('game-1', 'snapshot-1');

  expect(calls[0].url).toBe('/api/game-states/game-1/party/members/me/character');
  expect(calls[0].init?.method).toBe('POST');
  expect(calls[0].init?.body).toBe(JSON.stringify({ characterId: 'char-1' }));
  expect(calls[1].url).toBe('/api/game-states/game-1/snapshots');
  expect(calls[1].init?.body).toBe(JSON.stringify({ reason: 'before boss' }));
  expect(calls[2].url).toBe('/api/game-states/game-1/snapshots/snapshot-1/restore');
});
