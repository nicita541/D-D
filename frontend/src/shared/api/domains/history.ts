import { apiGet, apiPost } from '../client';
import type { JsonObject } from '../types';

const base = (gameStateId: string) => `/game-states/${gameStateId}`;

export const historyApi = {
  turns: (gameStateId: string) => apiGet<JsonObject[]>(`${base(gameStateId)}/turns`),
  turn: (gameStateId: string, turnId: string) => apiGet<JsonObject>(`${base(gameStateId)}/turns/${turnId}`),
  createTurn: (gameStateId: string, body: JsonObject) => apiPost<JsonObject>(`${base(gameStateId)}/turns`, body),
  changes: (gameStateId: string, status = '') => apiGet<JsonObject[]>(`${base(gameStateId)}/changes${status ? `?status=${encodeURIComponent(status)}` : ''}`),
  change: (gameStateId: string, changeId: string) => apiGet<JsonObject>(`${base(gameStateId)}/changes/${changeId}`),
  applyChange: (gameStateId: string, changeId: string) => apiPost<JsonObject>(`${base(gameStateId)}/changes/${changeId}/apply`, {}),
  rejectChange: (gameStateId: string, changeId: string, reason: string) => apiPost<JsonObject>(`${base(gameStateId)}/changes/${changeId}/reject`, { reason }),
  rolls: (gameStateId: string, limit = 50) => apiGet<JsonObject[]>(`${base(gameStateId)}/rolls?limit=${limit}`),
  roll: (gameStateId: string, body: JsonObject) => apiPost<JsonObject>(`${base(gameStateId)}/rolls`, body),
  checks: (gameStateId: string, limit = 50) => apiGet<JsonObject[]>(`${base(gameStateId)}/checks?limit=${limit}`),
  abilityCheck: (gameStateId: string, body: JsonObject) => apiPost<JsonObject>(`${base(gameStateId)}/checks/ability`, body),
  mechanicRequests: (gameStateId: string) => apiGet<JsonObject[]>(`${base(gameStateId)}/mechanic-requests`),
  resolveMechanicRequest: (gameStateId: string, requestId: string, body: JsonObject) =>
    apiPost<JsonObject>(`${base(gameStateId)}/mechanic-requests/${requestId}/resolve/ability-check`, body),
  aiContext: (gameStateId: string, recentEventsLimit = 10) =>
    apiGet<JsonObject>(`${base(gameStateId)}/ai-context?recentEventsLimit=${recentEventsLimit}`),
};
