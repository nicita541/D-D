import { useState } from 'react';
import { useParams } from 'react-router-dom';
import { characterDomainApi, charactersApi, economyApi, playApi } from '../shared/api/endpoints';
import { queryKeys } from '../shared/api/query-keys';
import type { JsonObject } from '../shared/api/types';
import { GameManageShell } from '../shared/components/GameManageShell';
import { ActionConsole, ReadOnlyDocument, ResourceManager } from '../shared/components/management';
import { Field, Panel } from '../shared/components/ui';
import { useQuery } from '@tanstack/react-query';
import { firstText, idOf, textOf } from '../shared/api/json';

const domains = {
  conditions: ['Состояния', { название: 'Новое состояние', тип: 'effect', постоянное: false, стаки: 1, эффекты: {}, теги: [] }],
  'limited-resources': ['Ограниченные ресурсы', { название: 'Новый ресурс', максимум: 1, текущее: 1, восстановление: 'long_rest' }],
  proficiencies: ['Владения', { название: 'Новое владение', тип: 'skill', бонус: 0 }],
  abilities: ['Способности', { название: 'Новая способность', описание: '', эффекты: {}, теги: [] }],
  inventory: ['Инвентарь', { name: 'Новый предмет', description: '', itemType: 'misc', quantity: 1, weight: 0, properties: {} }],
  attacks: ['Атаки', { название: 'Новая атака', описание: '', бонусАтаки: 0, урон: '1d6', типУрона: 'physical' }],
} satisfies Record<string, [string, JsonObject]>;

const singletonDomains = ['equipment', 'needs', 'progression', 'resources', 'attributes', 'wealth', 'combat-stats'] as const;

export function CharactersManagePage() {
  const { gameStateId = '' } = useParams();
  const characters = useQuery({ queryKey: queryKeys.characters(gameStateId), queryFn: () => charactersApi.list(gameStateId) });
  const [characterId, setCharacterId] = useState('');
  const [domain, setDomain] = useState<keyof typeof domains>('conditions');
  const activeId = characterId || idOf(characters.data?.[0]?.id) || '';
  const [domainTitle, defaultValue] = domains[domain];

  return (
    <GameManageShell title="Персонажи и снаряжение">
      <div className="manage-grid">
        <ResourceManager
          title="Основные данные персонажей"
          queryKey={queryKeys.characters(gameStateId)}
          list={() => charactersApi.list(gameStateId)}
          get={(id) => charactersApi.get(gameStateId, id)}
          create={(body) => charactersApi.create(gameStateId, body)}
          update={(id, body) => charactersApi.update(gameStateId, id, body)}
          remove={(id) => charactersApi.delete(gameStateId, id)}
          defaultValue={{ имя: 'Новый персонаж', вид: 'человек', класс: 'воин', описание: '' }}
          nameKey="имя"
          descriptionKey="описание"
        />
        <Panel title="Область редактирования">
          <div className="form-stack">
            <Field label="Персонаж">
              <select value={activeId} onChange={(event) => setCharacterId(event.target.value)}>
                {characters.data?.map((character) => (
                  <option key={idOf(character.id)} value={idOf(character.id)}>
                    {firstText(character, ['name', 'имя'], 'Персонаж')}
                  </option>
                ))}
              </select>
            </Field>
            <Field label="Раздел">
              <select value={domain} onChange={(event) => setDomain(event.target.value as keyof typeof domains)}>
                {Object.entries(domains).map(([key, [label]]) => <option key={key} value={key}>{label}</option>)}
              </select>
            </Field>
          </div>
        </Panel>
        {activeId ? (
          <ResourceManager
            key={`${activeId}-${domain}`}
            title={domainTitle}
            queryKey={queryKeys.characterDomain(gameStateId, activeId, domain)}
            list={() => characterDomainApi.list(gameStateId, activeId, domain)}
            create={(body) => characterDomainApi.create(gameStateId, activeId, domain, body)}
            update={domain === 'proficiencies' ? undefined : (id, body) => characterDomainApi.update(gameStateId, activeId, domain, id, body)}
            remove={(id) => characterDomainApi.delete(gameStateId, activeId, domain, id)}
            defaultValue={defaultValue}
            nameKey={domain === 'inventory' ? 'name' : 'название'}
            descriptionKey={domain === 'inventory' ? 'description' : 'описание'}
          />
        ) : null}
      </div>
      {activeId ? (
        <>
          <div className="manage-grid">
            <ReadOnlyDocument title="Прогрессия" queryKey={queryKeys.characterDomain(gameStateId, activeId, 'progression')} load={() => charactersApi.progression(gameStateId, activeId)} />
            <ReadOnlyDocument title="Экипировка" queryKey={queryKeys.characterDomain(gameStateId, activeId, 'equipment')} load={() => characterDomainApi.getObject(gameStateId, activeId, 'equipment')} />
            <ReadOnlyDocument title="Потребности" queryKey={queryKeys.characterDomain(gameStateId, activeId, 'needs')} load={() => characterDomainApi.getObject(gameStateId, activeId, 'needs')} />
            <ReadOnlyDocument title="Валюта" queryKey={queryKeys.characterDomain(gameStateId, activeId, 'currency')} load={() => economyApi.currency(gameStateId, activeId)} />
          </div>
          <h2 className="section-title">Ресурсы и действия персонажа</h2>
          <div className="command-grid">
            <ActionConsole title="Подготовить стартовый мир" actionLabel="Bootstrap" action={() => playApi.bootstrap(gameStateId)} />
            <ActionConsole title="Начать сюжетный ход" actionLabel="Начать" initialBody={{ сообщение: '' }} action={(body) => playApi.start(gameStateId, body)} />
            {singletonDomains.map((resource) => (
              <ActionConsole
                key={resource}
                title={`Обновить: ${resource}`}
                actionLabel="Сохранить"
                initialBody={{}}
                action={(body) => characterDomainApi.updateObject(gameStateId, activeId, resource, body)}
              />
            ))}
            <ActionConsole
              title="Экипировать / снять / использовать предмет"
              actionLabel="Выполнить"
              initialBody={{ itemId: '', action: 'equip' }}
              action={(body) => characterDomainApi.itemAction(gameStateId, activeId, textOf(body.itemId), textOf(body.action, 'equip') as 'equip' | 'unequip' | 'use')}
            />
            <ActionConsole
              title="Добавить валюту"
              actionLabel="Начислить"
              initialBody={{ copper: 0, silver: 0, gold: 0, platinum: 0, reason: '' }}
              action={(body) => economyApi.addCurrency(gameStateId, activeId, body)}
            />
            <ActionConsole
              title="Потратить валюту"
              actionLabel="Списать"
              initialBody={{ copper: 0, silver: 0, gold: 0, platinum: 0, reason: '' }}
              action={(body) => economyApi.spendCurrency(gameStateId, activeId, body)}
              danger
            />
            <ActionConsole
              title="Прогрессия"
              actionLabel="Начислить опыт"
              initialBody={{ amount: 100, reason: '' }}
              action={(body) => charactersApi.addXp(gameStateId, activeId, Number(body.amount ?? 0), textOf(body.reason))}
            />
            <ActionConsole title="Повысить уровень" actionLabel="Повысить" action={() => charactersApi.levelUp(gameStateId, activeId)} danger />
            <ActionConsole title="Продвинуть состояния" actionLabel="Tick" initialBody={{ turns: 1 }} action={(body) => charactersApi.tickConditions(gameStateId, activeId, Number(body.turns ?? 1))} />
            <ActionConsole title="Вывести из строя" actionLabel="Knockout" initialBody={{ reason: '' }} action={(body) => charactersApi.knockout(gameStateId, activeId, textOf(body.reason))} danger />
            <ActionConsole title="Вернуть в сознание" actionLabel="Revive" initialBody={{ hp: 1, reason: '' }} action={(body) => charactersApi.revive(gameStateId, activeId, Number(body.hp ?? 1), textOf(body.reason))} />
          </div>
        </>
      ) : null}
    </GameManageShell>
  );
}
