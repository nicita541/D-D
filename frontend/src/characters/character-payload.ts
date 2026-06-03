import type { JsonObject } from '../shared/api/types';

export interface CharacterFormData {
  name: string;
  species: string;
  className: string;
  background: string;
  description: string;
  strength: number;
  dexterity: number;
  constitution: number;
  intelligence: number;
  wisdom: number;
  charisma: number;
  hpMax: number;
  armorClass: number;
  gold: number;
}

export const defaultCharacter: CharacterFormData = {
  name: 'Торвен',
  species: 'человек',
  className: 'воин',
  background: 'Бывший страж каравана.',
  description: 'Путник с потёртым снаряжением и упрямым взглядом.',
  strength: 14,
  dexterity: 12,
  constitution: 13,
  intelligence: 10,
  wisdom: 11,
  charisma: 10,
  hpMax: 12,
  armorClass: 12,
  gold: 10,
};

export function toCreateCharacterPayload(data: CharacterFormData): JsonObject {
  return {
    имя: data.name,
    вид: data.species,
    класс: data.className,
    подкласс: null,
    предыстория: data.background,
    описание: data.description,
    мировоззрение: 'нейтральный добрый',
    прогресс: {
      уровень: 1,
      опыт: 0,
      опытДоСледующегоУровня: 300,
    },
    характеристики: {
      сила: data.strength,
      ловкость: data.dexterity,
      телосложение: data.constitution,
      интеллект: data.intelligence,
      мудрость: data.wisdom,
      харизма: data.charisma,
      инициатива: 0,
      скорость: 9,
      восприятие: 10,
    },
    ресурсы: {
      хпМаксимум: data.hpMax,
      хпТекущее: data.hpMax,
      манаМаксимум: 0,
      манаТекущая: 0,
      очкиДействийМаксимум: 1,
      очкиДействийТекущие: 1,
    },
    богатство: {
      медные: 0,
      серебряные: 0,
      золотые: data.gold,
      платиновые: 0,
    },
    бой: {
      классДоспеха: data.armorClass,
      бонусМастерства: 2,
      вБою: false,
      бросокИнициативы: 0,
    },
  };
}
