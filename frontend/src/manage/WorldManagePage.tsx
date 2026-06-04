import { useState } from 'react';
import { useParams } from 'react-router-dom';
import { worldApi, type WorldResource } from '../shared/api/endpoints';
import { queryKeys } from '../shared/api/query-keys';
import type { JsonObject } from '../shared/api/types';
import { GameManageShell } from '../shared/components/GameManageShell';
import { ActionConsole, ResourceManager } from '../shared/components/management';
import { Field, Panel } from '../shared/components/ui';
import { textOf } from '../shared/api/json';

const resources: Record<WorldResource, { label: string; nameKey: string; defaultValue: JsonObject }> = {
  locations: { label: 'Локации', nameKey: 'name', defaultValue: { name: 'Новая локация', description: '' } },
  objects: { label: 'Объекты', nameKey: 'name', defaultValue: { name: 'Новый объект', objectType: 'object', description: '', state: {}, tags: [] } },
  containers: { label: 'Контейнеры', nameKey: 'name', defaultValue: { name: 'Новый контейнер', description: '', isLocked: false } },
  npcs: { label: 'NPC', nameKey: 'name', defaultValue: { name: 'Новый NPC', role: '', attitude: 'neutral', description: '', isAlive: true } },
  factions: { label: 'Фракции', nameKey: 'name', defaultValue: { name: 'Новая фракция', reputation: 0, description: '' } },
  quests: { label: 'Квесты', nameKey: 'title', defaultValue: { title: 'Новый квест', description: '', status: 'active' } },
  monsters: { label: 'Монстры', nameKey: 'name', defaultValue: { name: 'Новый монстр', monsterType: 'beast', hpCurrent: 10, hpMax: 10, armorClass: 10, stats: {}, abilities: [], loot: [], tags: [] } },
};

export function WorldManagePage() {
  const { gameStateId = '' } = useParams();
  const [resource, setResource] = useState<WorldResource>('locations');
  const [parentId, setParentId] = useState('');
  const definition = resources[resource];

  return (
    <GameManageShell title="Мир кампании">
      <Panel title="Тип данных">
        <Field label="Раздел мира">
          <select value={resource} onChange={(event) => setResource(event.target.value as WorldResource)}>
            {Object.entries(resources).map(([key, item]) => <option key={key} value={key}>{item.label}</option>)}
          </select>
        </Field>
      </Panel>
      <ResourceManager
        key={resource}
        title={definition.label}
        queryKey={queryKeys.world(gameStateId, resource)}
        list={() => worldApi.list(gameStateId, resource)}
        get={resource === 'locations' || resource === 'monsters' ? (id) => worldApi.get(gameStateId, resource, id) : undefined}
        create={(body) => worldApi.create(gameStateId, resource, body)}
        update={(id, body) => worldApi.update(gameStateId, resource, id, body)}
        remove={(id) => worldApi.delete(gameStateId, resource, id)}
        defaultValue={definition.defaultValue}
        nameKey={definition.nameKey}
      />
      <h2 className="section-title">Связанные сущности</h2>
      <Panel title="Родительская запись">
        <Field label="ID локации или квеста">
          <input value={parentId} onChange={(event) => setParentId(event.target.value)} placeholder="UUID" />
        </Field>
      </Panel>
      {parentId ? (
        <div className="manage-grid">
          <ResourceManager
            title="Выходы локации"
            queryKey={queryKeys.world(gameStateId, 'exits', parentId)}
            list={() => worldApi.listChildren(gameStateId, 'locations', parentId, 'exits')}
            create={(body) => worldApi.createChild(gameStateId, 'locations', parentId, 'exits', body)}
            update={(id, body) => worldApi.updateChild(gameStateId, 'locations', parentId, 'exits', id, body)}
            remove={(id) => worldApi.deleteChild(gameStateId, 'locations', parentId, 'exits', id)}
            defaultValue={{ direction: 'north', targetLocationId: '', description: '', isLocked: false }}
            nameKey="direction"
          />
          <ResourceManager
            title="Шаги квеста"
            queryKey={queryKeys.world(gameStateId, 'steps', parentId)}
            list={() => worldApi.listChildren(gameStateId, 'quests', parentId, 'steps')}
            create={(body) => worldApi.createChild(gameStateId, 'quests', parentId, 'steps', body)}
            update={(id, body) => worldApi.updateChild(gameStateId, 'quests', parentId, 'steps', id, body)}
            remove={(id) => worldApi.deleteChild(gameStateId, 'quests', parentId, 'steps', id)}
            defaultValue={{ description: 'Новый шаг', isCompleted: false, sortOrder: 0 }}
            nameKey="description"
            descriptionKey="description"
          />
        </div>
      ) : null}
      <div className="command-grid">
        <ActionConsole title="Создать объект с произвольным payload" actionLabel="Создать" initialBody={{}} action={(body) => worldApi.create(gameStateId, resource, body)} />
        <ActionConsole title="Создать экземпляр монстра" actionLabel="Создать" initialBody={{ name: 'Монстр', monsterType: 'beast', hpMax: 10, armorClass: 10, locationId: null }} action={(body) => worldApi.spawnMonster(gameStateId, body)} />
        <ActionConsole title="Отметить монстра побеждённым" actionLabel="Победить" initialBody={{ monsterId: '', reason: '' }} action={(body) => worldApi.killMonster(gameStateId, textOf(body.monsterId), body)} danger />
      </div>
    </GameManageShell>
  );
}
