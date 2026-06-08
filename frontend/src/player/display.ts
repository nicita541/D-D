import type { AccountCharacter, CampaignTemplate, JsonObject } from '../shared/api/types';
import { firstText, numberOf, objectOf } from '../shared/api/json';

export function characterName(character: AccountCharacter | JsonObject | null | undefined) {
  return firstText(character as JsonObject | null | undefined, ['name', 'имя'], 'Герой');
}

export function characterSpecies(character: AccountCharacter | JsonObject | null | undefined) {
  return firstText(character as JsonObject | null | undefined, ['species', 'вид'], 'вид');
}

export function characterClass(character: AccountCharacter | JsonObject | null | undefined) {
  return firstText(character as JsonObject | null | undefined, ['className', 'class', 'класс'], 'класс');
}

export function characterLevel(character: AccountCharacter | JsonObject | null | undefined) {
  const progression = objectOf((character as AccountCharacter | undefined)?.progression ?? (character as JsonObject | undefined)?.progression);
  return numberOf(valueOf(progression, ['level', 'уровень']), 1);
}

export function characterHp(character: AccountCharacter | JsonObject | null | undefined) {
  const source = character as AccountCharacter | JsonObject | undefined;
  const resources = objectOf(source?.resources) ?? objectOf(source?.progression);
  return {
    current: numberOf(valueOf(resources, ['hpCurrent', 'хпТекущее']), 1),
    max: numberOf(valueOf(resources, ['hpMax', 'хпМаксимум']), 1),
  };
}

export function characterAc(character: AccountCharacter | JsonObject | null | undefined) {
  const combat = objectOf((character as AccountCharacter | undefined)?.combat ?? (character as JsonObject | undefined)?.combat);
  return numberOf(valueOf(combat, ['armorClass', 'классДоспеха']), 10);
}

export function characterGold(character: AccountCharacter | JsonObject | null | undefined) {
  const wealth = objectOf((character as AccountCharacter | undefined)?.wealth ?? (character as JsonObject | undefined)?.wealth);
  return numberOf(valueOf(wealth, ['gold', 'золотые']), 0);
}

export function campaignTitle(campaign: CampaignTemplate | JsonObject | null | undefined) {
  return firstText(campaign, ['title', 'название'], 'История');
}

export function campaignGenre(campaign: CampaignTemplate | JsonObject | null | undefined) {
  return firstText(campaign, ['genre', 'жанр'], 'приключение');
}

export function campaignTone(campaign: CampaignTemplate | JsonObject | null | undefined) {
  return firstText(campaign, ['tone', 'тон'], 'героический');
}

export function campaignSummary(campaign: CampaignTemplate | JsonObject | null | undefined) {
  return firstText(campaign, ['summary', 'краткоеОписание'], 'Короткое приключение для старта.');
}

export function campaignOpening(campaign: CampaignTemplate | JsonObject | null | undefined) {
  return firstText(campaign, ['openingScene', 'вступление'], '');
}

function valueOf(source: JsonObject | null, keys: string[]) {
  if (!source) return undefined;
  for (const key of keys) {
    if (source[key] !== undefined) return source[key];
  }
  return undefined;
}
