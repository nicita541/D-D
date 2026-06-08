import { apiGet, apiPost } from '../client';
import type { SnapshotDto } from '../types';

const base = (gameStateId: string) => `/game-states/${gameStateId}/snapshots`;

export const snapshotsApi = {
  list: (gameStateId: string) => apiGet<SnapshotDto[]>(base(gameStateId)),
  create: (gameStateId: string, reason: string) => apiPost<SnapshotDto>(base(gameStateId), { reason }),
  restore: (gameStateId: string, snapshotId: string) => apiPost<{ id: string; gameStateId: string; message: string }>(`${base(gameStateId)}/${snapshotId}/restore`, {}),
};
