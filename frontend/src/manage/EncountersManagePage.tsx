import { useParams } from 'react-router-dom';
import { directCombatApi, economyApi, lootApi, timeApi } from '../shared/api/endpoints';
import { queryKeys } from '../shared/api/query-keys';
import { textOf } from '../shared/api/json';
import { GameManageShell } from '../shared/components/GameManageShell';
import { ActionConsole, ReadOnlyDocument } from '../shared/components/management';

export function EncountersManagePage() {
  const { gameStateId = '' } = useParams();

  return (
    <GameManageShell title="Бои, добыча и награды">
      <div className="manage-grid">
        <ReadOnlyDocument title="Активный бой" queryKey={queryKeys.combat(gameStateId)} load={() => directCombatApi.get(gameStateId)} />
        <ReadOnlyDocument title="Исход боя" queryKey={[...queryKeys.combat(gameStateId), 'outcome']} load={() => directCombatApi.outcome(gameStateId)} />
        <ReadOnlyDocument title="Добыча" queryKey={queryKeys.loot(gameStateId)} load={() => lootApi.list(gameStateId)} />
        <ReadOnlyDocument title="Игровое время" queryKey={[...queryKeys.loot(gameStateId), 'time']} load={() => timeApi.get(gameStateId)} />
      </div>
      <div className="command-grid">
        <ActionConsole title="Начать бой" actionLabel="Начать" initialBody={{ roundNumber: 1 }} action={(body) => directCombatApi.start(gameStateId, body)} />
        <ActionConsole title="Добавить участника" actionLabel="Добавить" initialBody={{ типАктера: 'character', actorId: '', имя: '', инициатива: 10, хпТекущее: 10, хпМаксимум: 10, состояния: [] }} action={(body) => directCombatApi.addParticipant(gameStateId, body)} />
        <ActionConsole title="Следующий ход" actionLabel="Следующий" action={() => directCombatApi.nextTurn(gameStateId)} />
        <ActionConsole title="Нанести урон" actionLabel="Применить" initialBody={{ participantId: '', damage: 1, reason: '' }} action={(body) => directCombatApi.damage(gameStateId, body)} danger />
        <ActionConsole title="Вылечить участника" actionLabel="Вылечить" initialBody={{ participantId: '', amount: 1, reason: '' }} action={(body) => directCombatApi.heal(gameStateId, textOf(body.participantId), body)} />
        <ActionConsole title="Атака" actionLabel="Атаковать" initialBody={{ attackerParticipantId: '', targetParticipantId: '', attackId: null, attackBonus: 0, damageDice: '1d6', damageBonus: 0 }} action={(body) => directCombatApi.attack(gameStateId, body)} />
        <ActionConsole title="Завершить бой" actionLabel="Завершить" action={() => directCombatApi.end(gameStateId)} danger />
        <ActionConsole title="Создать добычу" actionLabel="Создать" initialBody={{ name: 'Добыча', sourceType: 'manual', items: [], currency: {} }} action={(body) => lootApi.create(gameStateId, body)} />
        <ActionConsole title="Забрать добычу" actionLabel="Забрать" initialBody={{ lootContainerId: '', characterId: '' }} action={(body) => lootApi.claim(gameStateId, textOf(body.lootContainerId), textOf(body.characterId))} />
        <ActionConsole title="Посмотреть контейнер добычи" actionLabel="Загрузить" initialBody={{ lootContainerId: '' }} action={(body) => lootApi.get(gameStateId, textOf(body.lootContainerId))} />
        <ActionConsole title="Завершить квест" actionLabel="Завершить" initialBody={{ questId: '' }} action={(body) => economyApi.completeQuest(gameStateId, textOf(body.questId))} danger />
        <ActionConsole title="Выдать награду квеста" actionLabel="Выдать" initialBody={{ questId: '', characterId: '', grantExperience: true, grantCurrency: true, grantItems: true }} action={(body) => economyApi.grantQuestReward(gameStateId, textOf(body.questId), body)} />
      </div>
    </GameManageShell>
  );
}
