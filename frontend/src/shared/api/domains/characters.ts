import { apiDelete, apiGet, apiPost, apiPut } from '../client';
import type { JsonObject, OperationResponse } from '../types';

const base = (gameStateId: string) => `/game-states/${gameStateId}/characters`;
const characterBase = (gameStateId: string, characterId: string) => `${base(gameStateId)}/${characterId}`;

export const charactersApi = {
  list: (gameStateId: string) => apiGet<JsonObject[]>(base(gameStateId)),
  get: (gameStateId: string, characterId: string) => apiGet<JsonObject>(characterBase(gameStateId, characterId)),
  create: (gameStateId: string, body: JsonObject) => apiPost<OperationResponse>(base(gameStateId), body),
  update: (gameStateId: string, characterId: string, body: JsonObject) => apiPut<OperationResponse>(characterBase(gameStateId, characterId), body),
  delete: (gameStateId: string, characterId: string) => apiDelete<OperationResponse>(characterBase(gameStateId, characterId)),
  progression: (gameStateId: string, characterId: string) => apiGet<JsonObject>(`${characterBase(gameStateId, characterId)}/progression`),
  addXp: (gameStateId: string, characterId: string, amount: number, reason: string) =>
    apiPost<JsonObject>(`${characterBase(gameStateId, characterId)}/experience`, { amount, reason }),
  levelUp: (gameStateId: string, characterId: string) => apiPost<JsonObject>(`${characterBase(gameStateId, characterId)}/level-up`, {}),
  tickConditions: (gameStateId: string, characterId: string, turns = 1) =>
    apiPost<JsonObject>(`${characterBase(gameStateId, characterId)}/conditions/tick`, { turns }),
  knockout: (gameStateId: string, characterId: string, reason = 'Выведен из строя через игровой экран.') =>
    apiPost<JsonObject>(`${characterBase(gameStateId, characterId)}/knockout`, { reason }),
  revive: (gameStateId: string, characterId: string, hp = 1, reason = 'Оживление через игровой экран.') =>
    apiPost<JsonObject>(`${characterBase(gameStateId, characterId)}/revive`, { hp, reason, clearDead: true }),
};

export const characterDomainApi = {
  list: (gameStateId: string, characterId: string, resource: string) =>
    apiGet<JsonObject[]>(`${characterBase(gameStateId, characterId)}/${resource}`),
  getObject: (gameStateId: string, characterId: string, resource: string) =>
    apiGet<JsonObject>(`${characterBase(gameStateId, characterId)}/${resource}`),
  create: (gameStateId: string, characterId: string, resource: string, body: JsonObject) =>
    apiPost<OperationResponse>(`${characterBase(gameStateId, characterId)}/${resource}`, body),
  update: (gameStateId: string, characterId: string, resource: string, itemId: string, body: JsonObject) =>
    apiPut<OperationResponse>(`${characterBase(gameStateId, characterId)}/${resource}/${itemId}`, body),
  updateObject: (gameStateId: string, characterId: string, resource: string, body: JsonObject) =>
    apiPut<OperationResponse>(`${characterBase(gameStateId, characterId)}/${resource}`, body),
  delete: (gameStateId: string, characterId: string, resource: string, itemId: string) =>
    apiDelete<OperationResponse>(`${characterBase(gameStateId, characterId)}/${resource}/${itemId}`),
  itemAction: (gameStateId: string, characterId: string, itemId: string, action: 'equip' | 'unequip' | 'use', body: JsonObject = {}) =>
    apiPost<OperationResponse>(`${characterBase(gameStateId, characterId)}/inventory/items/${itemId}/${action}`, body),
};

export const inventoryApi = {
  list: (gameStateId: string, characterId: string) => characterDomainApi.list(gameStateId, characterId, 'inventory'),
  create: (gameStateId: string, characterId: string, body: JsonObject) => characterDomainApi.create(gameStateId, characterId, 'inventory', body),
  equip: (gameStateId: string, characterId: string, itemId: string) => characterDomainApi.itemAction(gameStateId, characterId, itemId, 'equip'),
  unequip: (gameStateId: string, characterId: string, itemId: string) => characterDomainApi.itemAction(gameStateId, characterId, itemId, 'unequip'),
  use: (gameStateId: string, characterId: string, itemId: string) => characterDomainApi.itemAction(gameStateId, characterId, itemId, 'use'),
  delete: (gameStateId: string, characterId: string, itemId: string) => characterDomainApi.delete(gameStateId, characterId, 'inventory', itemId),
};
