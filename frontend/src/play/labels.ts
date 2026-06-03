const operationLabels: Record<string, string> = {
  add_journal_entry: 'Запись в журнал',
  update_memory: 'Обновление памяти',
  update_scene: 'Обновление сцены',
  request_roll: 'Запрос броска',
  запросить_бросок: 'Запрос броска',
  create_monster: 'Создать монстра',
  spawn_monster: 'Появление монстра',
  kill_monster: 'Убить монстра',
  add_xp: 'Начислить опыт',
  level_up: 'Повысить уровень',
  add_currency: 'Начислить валюту',
  spend_currency: 'Потратить валюту',
  complete_quest: 'Завершить квест',
  grant_reward: 'Выдать награду',
  grant_quest_reward: 'Выдать награду квеста',
  short_rest: 'Короткий отдых',
  long_rest: 'Долгий отдых',
  advance_time: 'Продвинуть время',
  tick_conditions: 'Обновить состояния',
  kill_character: 'Убить персонажа',
  revive_character: 'Оживить персонажа',
  knock_out_character: 'Вывести из строя',
};

const dangerousOperations = new Set([
  'request_roll',
  'запросить_бросок',
  'create_monster',
  'spawn_monster',
  'kill_monster',
  'add_xp',
  'level_up',
  'add_currency',
  'spend_currency',
  'complete_quest',
  'grant_reward',
  'grant_quest_reward',
  'short_rest',
  'long_rest',
  'advance_time',
  'tick_conditions',
  'kill_character',
  'revive_character',
  'knock_out_character',
]);

export function operationLabel(operation: string) {
  return operationLabels[operation] ?? operation;
}

export function operationDanger(operation: string) {
  return dangerousOperations.has(operation);
}

export function modeInfo(mode: string) {
  const info: Record<string, { label: string; text: string }> = {
    narration: { label: 'Повествование', text: 'Можно действовать.' },
    awaiting_roll: { label: 'Нужен бросок', text: 'Сначала нужно закрыть проверку.' },
    awaiting_change_confirmation: { label: 'Подтверждение изменений', text: 'Есть pending changes.' },
    combat: { label: 'Бой', text: 'Идёт боевое столкновение.' },
    travel: { label: 'Переход', text: 'Доступны перемещения.' },
  };

  return info[mode] ?? { label: mode, text: 'Состояние игры обновлено.' };
}
