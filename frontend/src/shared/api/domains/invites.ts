import { apiDelete, apiGet, apiPost } from '../client';
import type {
  InviteAcceptedResponse,
  InviteCreatedResponse,
  InvitePreviewResponse,
  OperationResponse,
} from '../types';

export const invitesApi = {
  create: (gameStateId: string, body: { role?: string; expiresInHours?: number; maxUses?: number } = {}) =>
    apiPost<InviteCreatedResponse>(`/game-states/${gameStateId}/invites`, body),
  preview: (token: string) => apiGet<InvitePreviewResponse>(`/invites/${encodeURIComponent(token)}`, { skipAuth: true }),
  accept: (token: string) => apiPost<InviteAcceptedResponse>(`/invites/${encodeURIComponent(token)}/accept`, {}),
  revoke: (gameStateId: string, inviteId: string) => apiDelete<OperationResponse>(`/game-states/${gameStateId}/invites/${inviteId}`),
};
