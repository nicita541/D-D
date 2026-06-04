import { describe, expect, it } from 'vitest';
import { normalizeTravelOptions } from './travel';

describe('normalizeTravelOptions', () => {
  it('uses exits from the travel options document', () => {
    const exits = [{ id: 'exit-1', targetLocationId: 'location-2' }];

    expect(normalizeTravelOptions({
      currentLocationId: 'location-1',
      exits,
      locations: [{ id: 'location-1' }, { id: 'location-2' }],
    })).toEqual(exits);
  });

  it('falls back to direct locations and excludes the current location', () => {
    expect(normalizeTravelOptions({
      currentLocationId: 'location-1',
      exits: [],
      locations: [{ id: 'location-1' }, { id: 'location-2' }],
    })).toEqual([{ id: 'location-2' }]);
  });
});
