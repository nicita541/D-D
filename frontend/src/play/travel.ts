import type { JsonObject } from '../shared/api/types';
import { arrayOfObjects, idOf, objectOf } from '../shared/api/json';

export function normalizeTravelOptions(value: unknown): JsonObject[] {
  const document = objectOf(value);
  if (!document) {
    return arrayOfObjects(value);
  }

  const exits = arrayOfObjects(document.exits);
  if (exits.length > 0) {
    return exits;
  }

  const currentLocationId = idOf(document.currentLocationId);
  return arrayOfObjects(document.locations).filter((location) => idOf(location.id) !== currentLocationId);
}
