export type JsonPrimitive = string | number | boolean | null;
export type JsonValue = JsonPrimitive | JsonObject | JsonValue[];

export interface JsonObject {
  [key: string]: JsonValue;
}

export interface AccountDto {
  id: string;
  email: string;
  username: string;
  displayName?: string | null;
  role: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  account: AccountDto;
}

export interface ApiErrorPayload {
  message?: string;
  error?: string;
  title?: string;
  detail?: string;
  [key: string]: unknown;
}

export class ApiError extends Error {
  public readonly status: number;
  public readonly payload?: unknown;

  constructor(status: number, message: string, payload?: unknown) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.payload = payload;
  }
}

export interface GameStateSummary {
  id: string;
  name?: string;
  название?: string;
  turnNumber?: number;
  currentLocationId?: string | null;
}

export interface OperationResponse {
  id: string;
  message: string;
}

export interface CampaignTemplate extends JsonObject {
  id: string;
  название: string;
  жанр: string | null;
  тон: string | null;
  краткоеописание: string | null;
  вступление: string | null;
  главнаяцель: string | null;
  секретымастера: string[];
  начальныефлаги: JsonObject;
}

export interface AccountCharacter {
  id: string;
  accountId: string;
  name: string;
  species?: string | null;
  className?: string | null;
  background?: string | null;
  description?: string | null;
  alignment?: string | null;
  attributes: JsonObject;
  resources: JsonObject;
  progression: JsonObject;
  wealth: JsonObject;
  combat: JsonObject;
  inventory: JsonObject[];
  equipment: JsonObject;
  attacks: JsonObject[];
  metadata: JsonObject;
  version: number;
  createdAt: string;
  updatedAt: string;
}

export interface GenerateAccountCharacterRequest {
  name?: string;
  species?: string;
  className?: string;
  background?: string;
  description?: string;
}

export interface StartGameSessionRequest {
  accountCharacterId: string;
  campaignTemplateId: string;
  mode: 'solo';
}

export interface StartGameSessionResponse {
  gameStateId: string;
  characterId: string;
  playUrl: string;
}

export interface PlayScene {
  title?: string | null;
  summary?: string | null;
  currentObjective?: string | null;
  currentThreat?: string | null;
  locationId?: string | null;
  activeNpcIds?: string[];
  activeQuestIds?: string[];
  updatedAt?: string | null;
}

export interface PlayChangeApplicationItem {
  id?: string | null;
  operation: string;
  result?: unknown;
  reason?: string | null;
  message?: string | null;
}

export interface PlayStateResponse {
  mode: string;
  masterAnswer?: string | null;
  gameState?: JsonObject | null;
  scene?: PlayScene | null;
  characters: JsonObject[];
  recentTurns: JsonObject[];
  pendingChanges: JsonObject[];
  mechanicRequests: JsonObject[];
  combat?: JsonObject | null;
  inventory: JsonObject[];
  appliedChanges: PlayChangeApplicationItem[];
  skippedChanges: PlayChangeApplicationItem[];
  failedChanges: PlayChangeApplicationItem[];
  memory?: JsonObject | null;
  monsters: JsonObject[];
  loot: JsonObject[];
  rewards: JsonObject[];
  progression?: JsonObject | null;
  currency?: JsonObject | null;
  time?: JsonObject | null;
  activeConditions: JsonObject[];
  restAvailable?: JsonObject | null;
  characterStates: JsonObject[];
  generatedAt: string;
  permissions?: PlayPermissions | null;
  currentPartyMember?: CurrentPartyMember | null;
}

export interface PlayPermissions {
  canRead: boolean;
  canPlay: boolean;
  canManage: boolean;
  canViewSecrets: boolean;
  canControlSelectedCharacter: boolean;
}

export interface CurrentPartyMember {
  id?: string | null;
  role: string;
  characterId?: string | null;
  isHost: boolean;
}

export interface InviteCreatedResponse {
  id: string;
  gameStateId: string;
  token: string;
  role: string;
  maxUses: number;
  expiresAt: string;
}

export interface InvitePreviewResponse {
  id: string;
  gameStateId: string;
  gameName: string;
  role: string;
  usesRemaining: number;
  expiresAt: string;
}

export interface InviteAcceptedResponse {
  gameStateId: string;
  partyMemberId: string;
  role: string;
  message: string;
}

export interface SnapshotDto {
  id: string;
  gameStateId: string;
  reason: string;
  createdByAccountId?: string | null;
  createdAt: string;
  restoredByAccountId?: string | null;
  restoredAt?: string | null;
}
