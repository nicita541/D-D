export interface Player {
  character: Character;
  resources: Resources;
  attributes: Attributes;
  inventory: string[];
}

export interface Character {
  name: string;
  background: string;
  species: string;
  class: string;
  subclass: string;
  level: number;
  experience: number;
}

export interface Resources {
  hp: Stat;
  mana: Stat;
  actionPoints: Stat;
}

export interface Stat {
  max: number;
  current: number;
}

export interface Attributes {
  strength: number;
  intelligence: number;
  dexterity: number;
  wisdom: number;
  constitution: number;
  charisma: number;
  initiative: number;
  speed: number;
  perception: number;
}