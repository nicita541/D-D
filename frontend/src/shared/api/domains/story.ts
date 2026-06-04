import { apiDelete, apiGet, apiPost, apiPut } from '../client';
import type { JsonObject, OperationResponse } from '../types';

const base = (gameStateId: string) => `/game-states/${gameStateId}`;

export const storyApi = {
  get: (gameStateId: string) => apiGet<JsonObject>(`${base(gameStateId)}/story`),
  update: (gameStateId: string, body: JsonObject) => apiPut<OperationResponse>(`${base(gameStateId)}/story`, body),
  getMemory: (gameStateId: string) => apiGet<JsonObject>(`${base(gameStateId)}/memory`),
  updateMemory: (gameStateId: string, body: JsonObject) => apiPut<JsonObject>(`${base(gameStateId)}/memory`, body),
  summarizeMemory: (gameStateId: string, body: JsonObject = {}) => apiPost<JsonObject>(`${base(gameStateId)}/memory/summarize`, body),
  getParty: (gameStateId: string) => apiGet<JsonObject>(`${base(gameStateId)}/party`),
  createParty: (gameStateId: string, body: JsonObject) => apiPost<OperationResponse>(`${base(gameStateId)}/party`, body),
  addPartyMember: (gameStateId: string, body: JsonObject) => apiPost<OperationResponse>(`${base(gameStateId)}/party/members`, body),
  removePartyMember: (gameStateId: string, memberId: string) => apiDelete<OperationResponse>(`${base(gameStateId)}/party/members/${memberId}`),
};
