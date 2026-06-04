import { apiDelete, apiGet, apiPost } from '../client';
import type { CampaignTemplate, GameStateSummary, JsonObject, OperationResponse } from '../types';

export const gameStatesApi = {
  list: () => apiGet<GameStateSummary[]>('/game-states'),
  get: (gameStateId: string) => apiGet<GameStateSummary>(`/game-states/${gameStateId}`),
  create: (name: string) => apiPost<OperationResponse>('/game-states', { название: name }),
  delete: (gameStateId: string) => apiDelete<OperationResponse>(`/game-states/${gameStateId}`),
};

export const campaignsApi = {
  list: () => apiGet<CampaignTemplate[]>('/campaigns'),
  get: (id: string) => apiGet<CampaignTemplate>(`/campaigns/${id}`),
  create: (body: JsonObject) => apiPost<OperationResponse>('/campaigns', body),
  delete: (id: string) => apiDelete<OperationResponse>(`/campaigns/${id}`),
};
