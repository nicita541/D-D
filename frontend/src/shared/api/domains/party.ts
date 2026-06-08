import { apiDelete, apiGet, apiPatch, apiPost } from '../client';
import type { JsonObject, OperationResponse } from '../types';

const base = (gameStateId: string) => `/game-states/${gameStateId}/party`;

export const partyApi = {
  get: (gameStateId: string) => apiGet<JsonObject>(base(gameStateId)),
  updateMember: (gameStateId: string, memberId: string, body: JsonObject) =>
    apiPatch<OperationResponse>(`${base(gameStateId)}/members/${memberId}`, body),
  assignMemberCharacter: (gameStateId: string, memberId: string, characterId: string | null) =>
    apiPost<OperationResponse>(`${base(gameStateId)}/members/${memberId}/character`, { characterId }),
  assignMyCharacter: (gameStateId: string, characterId: string) =>
    apiPost<OperationResponse>(`${base(gameStateId)}/members/me/character`, { characterId }),
  removeMember: (gameStateId: string, memberId: string) => apiDelete<OperationResponse>(`${base(gameStateId)}/members/${memberId}`),
  leave: (gameStateId: string) => apiDelete<OperationResponse>(`${base(gameStateId)}/members/me`),
};
