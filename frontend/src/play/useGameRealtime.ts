import { useEffect, useState } from 'react';
import { HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { useQueryClient } from '@tanstack/react-query';
import { apiBaseUrl } from '../shared/api/client';
import { readAuthSession } from '../shared/api/auth-storage';
import { queryKeys } from '../shared/api/query-keys';

type RealtimeStatus = 'online' | 'reconnecting' | 'polling';

const gameEvents = [
  'GameUpdated',
  'TurnAdded',
  'CombatUpdated',
  'PartyUpdated',
  'InventoryUpdated',
  'TravelUpdated',
  'RestCompleted',
  'InviteAccepted',
];

export function useGameRealtime(gameStateId: string) {
  const queryClient = useQueryClient();
  const [status, setStatus] = useState<RealtimeStatus>('polling');

  useEffect(() => {
    if (!gameStateId) {
      return;
    }

    const session = readAuthSession();
    if (!session?.accessToken) {
      return;
    }

    let disposed = false;
    const connection = new HubConnectionBuilder()
      .withUrl(`${hubBaseUrl()}/hubs/games`, { accessTokenFactory: () => readAuthSession()?.accessToken ?? '' })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    function invalidateAll() {
      void queryClient.invalidateQueries({ queryKey: queryKeys.play(gameStateId) });
      void queryClient.invalidateQueries({ queryKey: queryKeys.party(gameStateId) });
      void queryClient.invalidateQueries({ queryKey: queryKeys.snapshots(gameStateId) });
      void queryClient.invalidateQueries({ queryKey: queryKeys.combat(gameStateId) });
      void queryClient.invalidateQueries({ queryKey: queryKeys.loot(gameStateId) });
      void queryClient.invalidateQueries({ queryKey: queryKeys.history(gameStateId, 'turns') });
    }

    for (const eventName of gameEvents) {
      connection.on(eventName, invalidateAll);
    }

    connection.onreconnecting(() => setStatus('reconnecting'));
    connection.onreconnected(async () => {
      setStatus('online');
      await connection.invoke('JoinGame', gameStateId);
      invalidateAll();
    });
    connection.onclose(() => {
      if (!disposed) {
        setStatus('polling');
      }
    });

    connection
      .start()
      .then(async () => {
        if (disposed || connection.state !== HubConnectionState.Connected) {
          return;
        }

        await connection.invoke('JoinGame', gameStateId);
        setStatus('online');
      })
      .catch(() => setStatus('polling'));

    return () => {
      disposed = true;
      setStatus('polling');
      if (connection.state === HubConnectionState.Connected) {
        void connection.invoke('LeaveGame', gameStateId).finally(() => connection.stop());
      } else {
        void connection.stop();
      }
    };
  }, [gameStateId, queryClient]);

  return status;
}

function hubBaseUrl() {
  const base = apiBaseUrl();
  if (base.endsWith('/api')) {
    return base.slice(0, -4) || window.location.origin;
  }

  return base || window.location.origin;
}
