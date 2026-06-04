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
}
