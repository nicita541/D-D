import { useState } from 'react';
import { useParams } from 'react-router-dom';
import { historyApi } from '../shared/api/endpoints';
import { queryKeys } from '../shared/api/query-keys';
import { textOf } from '../shared/api/json';
import { GameManageShell } from '../shared/components/GameManageShell';
import { ActionConsole, ReadOnlyDocument } from '../shared/components/management';
import { Field, Panel } from '../shared/components/ui';

const resources = ['turns', 'changes', 'rolls', 'checks', 'mechanic-requests', 'ai-context'] as const;
type HistoryResource = (typeof resources)[number];

export function HistoryManagePage() {
  const { gameStateId = '' } = useParams();
  const [resource, setResource] = useState<HistoryResource>('turns');

  const loaders: Record<HistoryResource, () => Promise<unknown>> = {
    turns: () => historyApi.turns(gameStateId),
    changes: () => historyApi.changes(gameStateId),
    rolls: () => historyApi.rolls(gameStateId),
    checks: () => historyApi.checks(gameStateId),
    'mechanic-requests': () => historyApi.mechanicRequests(gameStateId),
    'ai-context': () => historyApi.aiContext(gameStateId),
  };

  return (
    <GameManageShell title="История, проверки и AI context">
      <Panel title="Просмотр журнала">
        <Field label="Раздел">
          <select value={resource} onChange={(event) => setResource(event.target.value as HistoryResource)}>
            {resources.map((item) => <option key={item}>{item}</option>)}
          </select>
        </Field>
      </Panel>
      <ReadOnlyDocument key={resource} title={resource} queryKey={queryKeys.history(gameStateId, resource)} load={loaders[resource]} />
      <div className="command-grid">
        <ActionConsole title="Создать прямой ход" actionLabel="Отправить" initialBody={{ сообщение: '' }} action={(body) => historyApi.createTurn(gameStateId, body)} />
        <ActionConsole title="Открыть ход" actionLabel="Загрузить" initialBody={{ turnId: '' }} action={(body) => historyApi.turn(gameStateId, textOf(body.turnId))} />
        <ActionConsole title="Открыть change" actionLabel="Загрузить" initialBody={{ changeId: '' }} action={(body) => historyApi.change(gameStateId, textOf(body.changeId))} />
        <ActionConsole title="Бросить кубики" actionLabel="Бросить" initialBody={{ notation: '1d20', modifier: 0, reason: '' }} action={(body) => historyApi.roll(gameStateId, body)} />
        <ActionConsole title="Проверка характеристики" actionLabel="Проверить" initialBody={{ characterId: '', ability: 'wisdom', difficultyClass: 10, reason: '' }} action={(body) => historyApi.abilityCheck(gameStateId, body)} />
        <ActionConsole title="Применить change" actionLabel="Применить" initialBody={{ changeId: '' }} action={(body) => historyApi.applyChange(gameStateId, textOf(body.changeId))} danger />
        <ActionConsole title="Отклонить change" actionLabel="Отклонить" initialBody={{ changeId: '', reason: '' }} action={(body) => historyApi.rejectChange(gameStateId, textOf(body.changeId), textOf(body.reason))} danger />
        <ActionConsole title="Закрыть mechanic request" actionLabel="Разрешить" initialBody={{ requestId: '', characterId: '', ability: 'wisdom' }} action={(body) => historyApi.resolveMechanicRequest(gameStateId, textOf(body.requestId), body)} />
      </div>
    </GameManageShell>
  );
}
