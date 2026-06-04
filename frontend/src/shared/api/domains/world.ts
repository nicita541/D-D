import { apiDelete, apiGet, apiPost, apiPut } from '../client';
import type { JsonObject, OperationResponse } from '../types';

export type WorldResource = 'locations' | 'objects' | 'containers' | 'npcs' | 'factions' | 'quests' | 'monsters';
const base = (gameStateId: string) => `/game-states/${gameStateId}/world`;

export const worldApi = {
  list: (gameStateId: string, resource: WorldResource) => apiGet<JsonObject[]>(`${base(gameStateId)}/${resource}`),
  get: (gameStateId: string, resource: WorldResource, id: string) => apiGet<JsonObject>(`${base(gameStateId)}/${resource}/${id}`),
  create: (gameStateId: string, resource: WorldResource, body: JsonObject) => apiPost<OperationResponse>(`${base(gameStateId)}/${resource}`, body),
  update: (gameStateId: string, resource: WorldResource, id: string, body: JsonObject) =>
    apiPut<OperationResponse>(`${base(gameStateId)}/${resource}/${id}`, body),
  delete: (gameStateId: string, resource: WorldResource, id: string) => apiDelete<OperationResponse>(`${base(gameStateId)}/${resource}/${id}`),
  listChildren: (gameStateId: string, resource: 'locations' | 'quests', parentId: string, child: 'exits' | 'steps') =>
    apiGet<JsonObject[]>(`${base(gameStateId)}/${resource}/${parentId}/${child}`),
  createChild: (gameStateId: string, resource: 'locations' | 'quests', parentId: string, child: 'exits' | 'steps', body: JsonObject) =>
    apiPost<OperationResponse>(`${base(gameStateId)}/${resource}/${parentId}/${child}`, body),
  updateChild: (gameStateId: string, resource: 'locations' | 'quests', parentId: string, child: 'exits' | 'steps', id: string, body: JsonObject) =>
    apiPut<OperationResponse>(`${base(gameStateId)}/${resource}/${parentId}/${child}/${id}`, body),
  deleteChild: (gameStateId: string, resource: 'locations' | 'quests', parentId: string, child: 'exits' | 'steps', id: string) =>
    apiDelete<OperationResponse>(`${base(gameStateId)}/${resource}/${parentId}/${child}/${id}`),
  spawnMonster: (gameStateId: string, body: JsonObject) => apiPost<JsonObject>(`/game-states/${gameStateId}/monsters/spawn`, body),
  killMonster: (gameStateId: string, monsterId: string, body: JsonObject = {}) => apiPost<JsonObject>(`/game-states/${gameStateId}/monsters/${monsterId}/kill`, body),
};
