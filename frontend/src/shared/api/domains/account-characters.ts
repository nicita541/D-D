import { apiDelete, apiGet, apiPost, apiPut } from '../client';
import type { AccountCharacter, GenerateAccountCharacterRequest, OperationResponse } from '../types';

export const accountCharactersApi = {
  list: () => apiGet<AccountCharacter[]>('/characters'),
  get: (id: string) => apiGet<AccountCharacter>(`/characters/${id}`),
  generate: (body: GenerateAccountCharacterRequest = {}) => apiPost<AccountCharacter>('/characters/generate', { ...body }),
  update: (id: string, body: Record<string, unknown>) => apiPut<AccountCharacter>(`/characters/${id}`, body),
  delete: (id: string) => apiDelete<OperationResponse>(`/characters/${id}`),
};
