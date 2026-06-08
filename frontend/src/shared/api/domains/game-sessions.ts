import { apiPost } from '../client';
import type { StartGameSessionRequest, StartGameSessionResponse } from '../types';

export const gameSessionsApi = {
  start: (body: StartGameSessionRequest) => apiPost<StartGameSessionResponse>('/game-sessions/start', { ...body }),
};
