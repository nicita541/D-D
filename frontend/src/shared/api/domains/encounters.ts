import { apiGet, apiPost } from '../client';
import type { JsonObject, OperationResponse, PlayStateResponse } from '../types';

const base = (gameStateId: string) => `/game-states/${gameStateId}`;

export const directCombatApi = {
  get: (gameStateId: string) => apiGet<JsonObject>(`${base(gameStateId)}/combat`),
  start: (gameStateId: string, body: JsonObject) => apiPost<OperationResponse>(`${base(gameStateId)}/combat/start`, body),
  end: (gameStateId: string) => apiPost<OperationResponse>(`${base(gameStateId)}/combat/end`, {}),
  addParticipant: (gameStateId: string, body: JsonObject) => apiPost<OperationResponse>(`${base(gameStateId)}/combat/participants`, body),
  nextTurn: (gameStateId: string) => apiPost<JsonObject>(`${base(gameStateId)}/combat/next-turn`, {}),
  damage: (gameStateId: string, body: JsonObject) => apiPost<JsonObject>(`${base(gameStateId)}/combat/apply-damage`, body),
  heal: (gameStateId: string, participantId: string, body: JsonObject) =>
    apiPost<JsonObject>(`${base(gameStateId)}/combat/participants/${participantId}/heal`, body),
  attack: (gameStateId: string, body: JsonObject) => apiPost<JsonObject>(`${base(gameStateId)}/combat/attack`, body),
  outcome: (gameStateId: string) => apiGet<JsonObject>(`${base(gameStateId)}/combat/outcome`),
};

export const combatApi = {
  start: (gameStateId: string) => apiPost<PlayStateResponse>(`${base(gameStateId)}/play/combat/start`, { autoAddParty: true }),
  action: (gameStateId: string, attack: JsonObject) => apiPost<PlayStateResponse>(`${base(gameStateId)}/play/combat/action`, { action: 'attack', attack }),
  continue: (gameStateId: string) => apiPost<PlayStateResponse>(`${base(gameStateId)}/play/combat/continue`, {}),
  end: (gameStateId: string) => apiPost<PlayStateResponse>(`${base(gameStateId)}/play/combat/end`, {}),
  resolveOutcome: (gameStateId: string) => apiPost<PlayStateResponse>(`${base(gameStateId)}/play/combat/resolve-outcome`, { autoGrantRewards: true }),
};

export const lootApi = {
  list: (gameStateId: string) => apiGet<JsonObject[]>(`${base(gameStateId)}/loot`),
  get: (gameStateId: string, id: string) => apiGet<JsonObject>(`${base(gameStateId)}/loot/${id}`),
  create: (gameStateId: string, body: JsonObject) => apiPost<JsonObject>(`${base(gameStateId)}/loot`, body),
  claim: (gameStateId: string, id: string, characterId?: string) => apiPost<JsonObject>(`${base(gameStateId)}/loot/${id}/claim`, { characterId }),
};

export const economyApi = {
  currency: (gameStateId: string, characterId: string) => apiGet<JsonObject>(`${base(gameStateId)}/characters/${characterId}/currency`),
  addCurrency: (gameStateId: string, characterId: string, body: JsonObject) =>
    apiPost<JsonObject>(`${base(gameStateId)}/characters/${characterId}/currency/add`, body),
  spendCurrency: (gameStateId: string, characterId: string, body: JsonObject) =>
    apiPost<JsonObject>(`${base(gameStateId)}/characters/${characterId}/currency/spend`, body),
  completeQuest: (gameStateId: string, questId: string) => apiPost<JsonObject>(`${base(gameStateId)}/quests/${questId}/complete`, {}),
  grantQuestReward: (gameStateId: string, questId: string, body: JsonObject) =>
    apiPost<JsonObject>(`${base(gameStateId)}/quests/${questId}/rewards/grant`, body),
};

export const timeApi = {
  get: (gameStateId: string) => apiGet<JsonObject>(`${base(gameStateId)}/time`),
  advance: (gameStateId: string, minutes: number, reason = 'Игровое время продвинуто через игровой экран.') =>
    apiPost<JsonObject>(`${base(gameStateId)}/time/advance`, { minutes, reason, tickConditions: true }),
};

export const restApi = {
  short: (gameStateId: string, characterId?: string) => apiPost<JsonObject>(`${base(gameStateId)}/rest/short`, { characterId, reason: 'Короткий отдых.' }),
  long: (gameStateId: string, characterId?: string) => apiPost<JsonObject>(`${base(gameStateId)}/rest/long`, { characterId, reason: 'Долгий отдых.' }),
};
