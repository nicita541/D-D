import type { JsonObject } from './types';

export function textOf(value: unknown, fallback = '') {
  if (typeof value === 'string') return value;
  if (typeof value === 'number' || typeof value === 'boolean') return String(value);
  return fallback;
}

export function numberOf(value: unknown, fallback = 0) {
  if (typeof value === 'number' && Number.isFinite(value)) return value;
  if (typeof value === 'string') {
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : fallback;
  }
  return fallback;
}

export function boolOf(value: unknown, fallback = false) {
  return typeof value === 'boolean' ? value : fallback;
}

export function idOf(value: unknown) {
  return typeof value === 'string' && value.length > 0 ? value : undefined;
}

export function firstText(source: JsonObject | null | undefined, keys: string[], fallback = '') {
  if (!source) return fallback;
  for (const key of keys) {
    const value = source[key];
    if (typeof value === 'string' && value.trim()) {
      return value;
    }
  }
  return fallback;
}

export function objectOf(value: unknown): JsonObject | null {
  if (value && typeof value === 'object' && !Array.isArray(value)) {
    return value as JsonObject;
  }
  return null;
}

export function arrayOfObjects(value: unknown): JsonObject[] {
  return Array.isArray(value) ? value.filter((item): item is JsonObject => Boolean(objectOf(item))) : [];
}
