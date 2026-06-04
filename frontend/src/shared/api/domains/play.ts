import { apiGet, apiPost } from '../client';
import type { JsonObject, PlayStateResponse } from '../types';

const base = (gameStateId: string) => `/game-states/${gameStateId}/play`;

export const playApi = {
  status: (gameStateId: string) => apiGet<PlayStateResponse>(`${base(gameStateId)}/status`),
  bootstrap: (gameStateId: string) => apiPost<PlayStateResponse | JsonObject>(`${base(gameStateId)}/bootstrap`, {}),
  start: (gameStateId: string, body: JsonObject = {}) => apiPost<PlayStateResponse>(`${base(gameStateId)}/start`, body),
  message: (gameStateId: string, body: JsonObject) => apiPost<PlayStateResponse>(`${base(gameStateId)}/message`, body),
  act: (gameStateId: string, body: { message: string; characterId?: string; autoApplySafeChanges?: boolean; note?: string }) =>
    apiPost<PlayStateResponse>(`${base(gameStateId)}/act`, body),
  continue: (gameStateId: string, message?: string) => apiPost<PlayStateResponse>(`${base(gameStateId)}/continue`, message ? { message } : {}),
  resolveAndContinue: (gameStateId: string, requestId: string, characterId?: string) =>
    apiPost<PlayStateResponse>(`${base(gameStateId)}/resolve-and-continue/${requestId}`, { characterId }),
  applySafeChanges: (gameStateId: string) => apiPost<PlayStateResponse | JsonObject>(`${base(gameStateId)}/apply-safe-changes`, {}),
  applyChange: (gameStateId: string, changeId: string) => apiPost<JsonObject>(`${base(gameStateId)}/apply-change/${changeId}`, {}),
  rejectChange: (gameStateId: string, changeId: string, reason: string) => apiPost<JsonObject>(`${base(gameStateId)}/reject-change/${changeId}`, { reason }),
  travel: (gameStateId: string, targetLocationId: string, exitId?: string) =>
    apiPost<PlayStateResponse>(`${base(gameStateId)}/travel`, { targetLocationId, exitId }),
  combat: (gameStateId: string, action: 'start' | 'end' | 'continue' | 'resolve-outcome', body: JsonObject = {}) =>
    apiPost<PlayStateResponse>(`${base(gameStateId)}/combat/${action}`, body),
  combatAction: (gameStateId: string, body: JsonObject) => apiPost<PlayStateResponse>(`${base(gameStateId)}/combat/action`, body),
  summarize: (gameStateId: string, body: JsonObject = {}) => apiPost<JsonObject>(`${base(gameStateId)}/summarize`, body),
};

export const travelApi = {
  options: (gameStateId: string) => apiGet<JsonObject[]>(`/game-states/${gameStateId}/travel/options`),
  travel: (gameStateId: string, targetLocationId: string, exitId?: string) => playApi.travel(gameStateId, targetLocationId, exitId),
};
