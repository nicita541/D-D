import { saveAuthSession } from './auth-storage';
import { apiDelete, apiGet, apiPost } from './client';
import type { AuthResponse, GameStateSummary, JsonObject, PlayStateResponse } from './types';

export const authApi = {
  async register(body: { email: string; username: string; password: string; displayName?: string }) {
    const response = await apiPost<AuthResponse>('/auth/register', body, { skipAuth: true });
    return saveAuthSession(response);
  },

  async login(body: { emailOrUsername: string; password: string }) {
    const response = await apiPost<AuthResponse>('/auth/login', body, { skipAuth: true });
    return saveAuthSession(response);
  },

  me() {
    return apiGet<AuthResponse['account']>('/auth/me');
  },

  logout(refreshToken: string) {
    return apiPost('/auth/logout', { refreshToken });
  },
};

export const gameStatesApi = {
  list() {
    return apiGet<GameStateSummary[]>('/game-states');
  },

  get(gameStateId: string) {
    return apiGet<GameStateSummary>(`/game-states/${gameStateId}`);
  },

  create(name: string) {
    return apiPost<GameStateSummary>('/game-states', { name });
  },
};

export const charactersApi = {
  list(gameStateId: string) {
    return apiGet<JsonObject[]>(`/game-states/${gameStateId}/characters`);
  },

  create(gameStateId: string, body: JsonObject) {
    return apiPost<JsonObject>(`/game-states/${gameStateId}/characters`, body);
  },

  progression(gameStateId: string, characterId: string) {
    return apiGet<JsonObject>(`/game-states/${gameStateId}/characters/${characterId}/progression`);
  },

  addXp(gameStateId: string, characterId: string, amount: number, reason: string) {
    return apiPost<JsonObject>(`/game-states/${gameStateId}/characters/${characterId}/xp`, { amount, reason });
  },

  levelUp(gameStateId: string, characterId: string) {
    return apiPost<JsonObject>(`/game-states/${gameStateId}/characters/${characterId}/level-up`, {});
  },
};

export const playApi = {
  status(gameStateId: string) {
    return apiGet<PlayStateResponse>(`/game-states/${gameStateId}/play/status`);
  },

  bootstrap(gameStateId: string) {
    return apiPost<PlayStateResponse | JsonObject>(`/game-states/${gameStateId}/play/bootstrap`, {});
  },

  act(gameStateId: string, body: { message: string; characterId?: string; autoApplySafeChanges?: boolean; note?: string }) {
    return apiPost<PlayStateResponse>(`/game-states/${gameStateId}/play/act`, body);
  },

  continue(gameStateId: string, message?: string) {
    return apiPost<PlayStateResponse>(`/game-states/${gameStateId}/play/continue`, message ? { message } : {});
  },

  resolveAndContinue(gameStateId: string, requestId: string, characterId?: string) {
    return apiPost<PlayStateResponse>(`/game-states/${gameStateId}/play/resolve-and-continue/${requestId}`, { characterId });
  },

  applySafeChanges(gameStateId: string) {
    return apiPost<PlayStateResponse | JsonObject>(`/game-states/${gameStateId}/play/apply-safe-changes`, {});
  },

  applyChange(gameStateId: string, changeId: string) {
    return apiPost<JsonObject>(`/game-states/${gameStateId}/play/apply-change/${changeId}`, {});
  },

  rejectChange(gameStateId: string, changeId: string, reason: string) {
    return apiPost<JsonObject>(`/game-states/${gameStateId}/play/reject-change/${changeId}`, { reason });
  },
};

export const inventoryApi = {
  list(gameStateId: string, characterId: string) {
    return apiGet<JsonObject[]>(`/game-states/${gameStateId}/characters/${characterId}/inventory`);
  },

  create(gameStateId: string, characterId: string, body: JsonObject) {
    return apiPost<JsonObject>(`/game-states/${gameStateId}/characters/${characterId}/inventory/items`, body);
  },

  equip(gameStateId: string, characterId: string, itemId: string) {
    return apiPost<JsonObject>(`/game-states/${gameStateId}/characters/${characterId}/inventory/items/${itemId}/equip`, {});
  },

  unequip(gameStateId: string, characterId: string, itemId: string) {
    return apiPost<JsonObject>(`/game-states/${gameStateId}/characters/${characterId}/inventory/items/${itemId}/unequip`, {});
  },

  use(gameStateId: string, characterId: string, itemId: string) {
    return apiPost<JsonObject>(`/game-states/${gameStateId}/characters/${characterId}/inventory/items/${itemId}/use`, {});
  },

  delete(gameStateId: string, characterId: string, itemId: string) {
    return apiDelete<JsonObject>(`/game-states/${gameStateId}/characters/${characterId}/inventory/items/${itemId}`);
  },
};

export const travelApi = {
  options(gameStateId: string) {
    return apiGet<JsonObject[]>(`/game-states/${gameStateId}/travel/options`);
  },

  travel(gameStateId: string, targetLocationId: string, exitId?: string) {
    return apiPost<PlayStateResponse>(`/game-states/${gameStateId}/play/travel`, {
      targetLocationId,
      exitId,
      note: exitId ? 'Переход через выбранный выход.' : 'Прямой переход с игрового экрана.',
    });
  },
};

export const combatApi = {
  start(gameStateId: string) {
    return apiPost<PlayStateResponse>(`/game-states/${gameStateId}/play/combat/start`, { autoAddParty: true });
  },

  action(gameStateId: string, attack: JsonObject) {
    return apiPost<PlayStateResponse>(`/game-states/${gameStateId}/play/combat/action`, { action: 'attack', attack });
  },

  end(gameStateId: string) {
    return apiPost<PlayStateResponse>(`/game-states/${gameStateId}/play/combat/end`, {});
  },

  resolveOutcome(gameStateId: string) {
    return apiPost<PlayStateResponse>(`/game-states/${gameStateId}/play/combat/resolve-outcome`, { autoGrantRewards: true });
  },
};

export const lootApi = {
  claim(gameStateId: string, lootContainerId: string, characterId?: string) {
    return apiPost<JsonObject>(`/game-states/${gameStateId}/loot/${lootContainerId}/claim`, { characterId });
  },
};
