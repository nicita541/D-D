import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor } from '@testing-library/react';
import { afterEach, expect, it, vi } from 'vitest';
import { saveAuthSession } from '../shared/api/auth-storage';
import { queryKeys } from '../shared/api/query-keys';
import { useGameRealtime } from './useGameRealtime';

const signalrMock = vi.hoisted(() => {
  const handlers = new Map<string, (...args: unknown[]) => void>();
  const connection = {
    state: 'Connected',
    on: vi.fn((eventName: string, handler: (...args: unknown[]) => void) => {
      handlers.set(eventName, handler);
    }),
    onreconnecting: vi.fn(),
    onreconnected: vi.fn(),
    onclose: vi.fn(),
    start: vi.fn(() => Promise.resolve()),
    invoke: vi.fn(() => Promise.resolve()),
    stop: vi.fn(() => Promise.resolve()),
  };

  return { connection, handlers };
});

vi.mock('@microsoft/signalr', () => ({
  HubConnectionState: { Connected: 'Connected' },
  LogLevel: { Warning: 2 },
  HubConnectionBuilder: class {
    withUrl() {
      return this;
    }

    withAutomaticReconnect() {
      return this;
    }

    configureLogging() {
      return this;
    }

    build() {
      return signalrMock.connection;
    }
  },
}));

afterEach(() => {
  window.localStorage.clear();
  signalrMock.handlers.clear();
  vi.clearAllMocks();
});

it('joins the game hub and invalidates game query keys on realtime events', async () => {
  const gameStateId = 'game-1';
  saveAuthSession({
    accessToken: 'token',
    refreshToken: 'refresh',
    accessTokenExpiresAt: new Date().toISOString(),
    account: { id: 'account', email: 'a@example.com', username: 'a', role: 'user' },
  });
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  const invalidate = vi.spyOn(queryClient, 'invalidateQueries');

  render(
    <QueryClientProvider client={queryClient}>
      <RealtimeProbe gameStateId={gameStateId} />
    </QueryClientProvider>,
  );

  await waitFor(() => expect(screen.getByText('online')).toBeTruthy());
  expect(signalrMock.connection.invoke).toHaveBeenCalledWith('JoinGame', gameStateId);

  signalrMock.handlers.get('GameUpdated')?.({ gameStateId });

  expect(invalidate).toHaveBeenCalledWith({ queryKey: queryKeys.play(gameStateId) });
  expect(invalidate).toHaveBeenCalledWith({ queryKey: queryKeys.party(gameStateId) });
  expect(invalidate).toHaveBeenCalledWith({ queryKey: queryKeys.snapshots(gameStateId) });
  expect(invalidate).toHaveBeenCalledWith({ queryKey: queryKeys.combat(gameStateId) });
  expect(invalidate).toHaveBeenCalledWith({ queryKey: queryKeys.loot(gameStateId) });
  expect(invalidate).toHaveBeenCalledWith({ queryKey: queryKeys.history(gameStateId, 'turns') });
});

function RealtimeProbe({ gameStateId }: { gameStateId: string }) {
  const status = useGameRealtime(gameStateId);
  return <span>{status}</span>;
}
