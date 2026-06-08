import { useMemo, useState, type FormEvent } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { ArrowLeft, Dice5, Loader2, Play, RotateCw, Send, Sparkles } from 'lucide-react';
import { ApiError, type JsonObject, type PlayPermissions, type PlayStateResponse } from '../shared/api/types';
import { playApi } from '../shared/api/endpoints';
import { queryKeys } from '../shared/api/query-keys';
import { arrayOfObjects, firstText, idOf, numberOf, objectOf, textOf } from '../shared/api/json';
import { AppShell, Button, EmptyState, ErrorState, LoadingState, Panel } from '../shared/components/ui';
import { getErrorMessage } from '../shared/api/errors';
import { characterAc, characterClass, characterHp, characterLevel, characterName, characterSpecies } from '../player/display';
import { useGameRealtime } from './useGameRealtime';

export function PlayPage() {
  const { gameStateId = '' } = useParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const realtimeStatus = useGameRealtime(gameStateId);
  const [message, setMessage] = useState('');
  const [dice, setDice] = useState<number | null>(null);
  const [startError, setStartError] = useState('');

  const status = useQuery({
    queryKey: queryKeys.play(gameStateId),
    queryFn: () => playApi.status(gameStateId),
    refetchInterval: 10000,
    enabled: Boolean(gameStateId),
  });

  const state = useMemo(() => normalizePlayState(status.data), [status.data]);
  const permissions = state?.permissions ?? defaultPermissions;
  const activeCharacterId = idOf(state?.currentPartyMember?.characterId) || idOf(state?.characters[0]?.id) || '';
  const character = state?.characters.find((item) => idOf(item.id) === activeCharacterId) ?? state?.characters[0] ?? null;
  const canAct = Boolean(permissions.canPlay && activeCharacterId && (permissions.canManage || permissions.canControlSelectedCharacter));
  const hasStarted = Boolean(state && (state.masterAnswer || state.recentTurns.some(hasMasterAnswer)));

  function refresh() {
    void queryClient.invalidateQueries({ queryKey: queryKeys.play(gameStateId) });
  }

  const start = useMutation({
    mutationFn: async () => {
      setStartError('');
      try {
        await playApi.bootstrap(gameStateId);
      } catch (error) {
        if (!(error instanceof ApiError) || error.status !== 409) {
          throw error;
        }
      }

      return playApi.start(gameStateId, {});
    },
    onSuccess: refresh,
    onError: (error) => setStartError(getErrorMessage(error)),
  });

  const act = useMutation({
    mutationFn: (text: string) =>
      playApi.act(gameStateId, {
        characterId: activeCharacterId,
        message: text,
        autoApplySafeChanges: true,
      }),
    onSuccess: () => {
      setMessage('');
      refresh();
    },
  });

  const resolveRoll = useMutation({
    mutationFn: ({ requestId, roll }: { requestId: string; roll: number }) =>
      playApi.resolveAndContinue(gameStateId, requestId, activeCharacterId, roll),
    onSuccess: refresh,
  });

  function submit(event: FormEvent) {
    event.preventDefault();
    const text = message.trim();
    if (!text || !canAct || !hasStarted) return;
    act.mutate(text);
  }

  function rollD20() {
    const value = Math.floor(Math.random() * 20) + 1;
    setDice(value);
    return value;
  }

  if (status.isLoading) {
    return (
      <AppShell title="Игровой стол" actions={<BackButton onClick={() => navigate('/home')} />}>
        <LoadingState text="Загружаем стол..." />
      </AppShell>
    );
  }

  if (status.error) {
    return (
      <AppShell title="Игровой стол" actions={<BackButton onClick={() => navigate('/home')} />}>
        <ErrorState error={status.error} />
      </AppShell>
    );
  }

  if (!state) {
    return (
      <AppShell title="Игровой стол" actions={<BackButton onClick={() => navigate('/home')} />}>
        <EmptyState title="Состояние игры не найдено" />
      </AppShell>
    );
  }

  return (
    <AppShell
      title="Игровой стол"
      subtitle={`realtime: ${realtimeLabel(realtimeStatus)} · обновлено ${new Date(state.generatedAt).toLocaleTimeString()}`}
      actions={
        <>
          <Button variant="secondary" onClick={refresh}>
            <RotateCw size={18} /> Обновить
          </Button>
          <BackButton onClick={() => navigate('/home')} />
        </>
      }
    >
      <div className="player-table-grid">
        <section className={`chat-panel panel ${hasStarted ? 'active' : 'inactive'}`}>
          <div className="chat-header">
            <div>
              <p className="eyebrow">мастер игры</p>
              <h2>{hasStarted ? 'История идёт' : 'Чат пока не активен'}</h2>
            </div>
            {!hasStarted ? (
              <Button disabled={start.isPending || !permissions.canPlay} onClick={() => start.mutate()}>
                {start.isPending ? <Loader2 className="spin" size={18} /> : <Play size={18} />}
                {start.isPending ? 'Мастер готовит сцену...' : 'Начать'}
              </Button>
            ) : null}
          </div>

          {!hasStarted ? (
            <div className="inactive-chat">
              <p>Нажми «Начать», и мастер откроет первую сцену выбранной истории.</p>
              <p className="muted">Если Ollama временно недоступна, здесь появится понятная ошибка и можно будет повторить.</p>
              {startError ? <p className="form-error">{startError}</p> : null}
            </div>
          ) : (
            <>
              <ChatLog state={state} />
              <form className="chat-form" onSubmit={submit}>
                <textarea
                  value={message}
                  disabled={!canAct || act.isPending}
                  onChange={(event) => setMessage(event.target.value)}
                  placeholder={canAct ? 'Что делает герой?' : 'Для действий нужен назначенный герой.'}
                />
                <Button type="submit" disabled={!canAct || !message.trim() || act.isPending}>
                  {act.isPending ? <Loader2 className="spin" size={18} /> : <Send size={18} />}
                  Отправить
                </Button>
              </form>
              {act.error ? <p className="form-error">{getErrorMessage(act.error)}</p> : null}
            </>
          )}
        </section>

        <aside className="table-side">
          <HeroPanel character={character} state={state} />
          <DicePanel
            dice={dice}
            onRoll={rollD20}
            state={state}
            canAct={canAct}
            resolving={resolveRoll.isPending}
            onResolve={(requestId) => resolveRoll.mutate({ requestId, roll: rollD20() })}
          />
          <QuickActions disabled={!canAct || !hasStarted || act.isPending} onPick={(text) => act.mutate(text)} />
          <WorldStatus state={state} />
          {startError ? <p className="form-error">{startError}</p> : null}
          {resolveRoll.error ? <p className="form-error">{getErrorMessage(resolveRoll.error)}</p> : null}
        </aside>
      </div>
    </AppShell>
  );
}

export function ChatLog({ state }: { state: PlayStateResponse }) {
  const turns = state.recentTurns.slice().reverse().filter(hasVisibleChatMessage);
  if (turns.length === 0 && state.masterAnswer) {
    return (
      <div className="chat-log">
        <div className="message-bubble master">{state.masterAnswer}</div>
      </div>
    );
  }

  return (
    <div className="chat-log">
      {turns.map((turn, index) => {
        const player = firstText(turn, ['playerMessage', 'player_message', 'сообщениеИгрока'], '');
        const master = firstText(turn, ['masterAnswer', 'master_answer', 'ответМастера'], '');
        return (
          <div className="turn-block" key={String(turn.id ?? index)}>
            {player ? <div className="message-bubble player">{player}</div> : null}
            {master ? <div className="message-bubble master">{master}</div> : null}
          </div>
        );
      })}
    </div>
  );
}

function hasVisibleChatMessage(turn: JsonObject) {
  return Boolean(
    firstText(turn, ['playerMessage', 'player_message', 'сообщениеИгрока'], '')
    || firstText(turn, ['masterAnswer', 'master_answer', 'ответМастера'], ''),
  );
}

function hasMasterAnswer(turn: JsonObject) {
  return Boolean(firstText(turn, ['masterAnswer', 'master_answer', 'ответМастера'], ''));
}

function HeroPanel({ character, state }: { character: JsonObject | null; state: PlayStateResponse }) {
  if (!character) {
    return (
      <Panel title="Герой">
        <EmptyState title="Герой не назначен" text="Вернись в меню и запусти историю через постоянного персонажа." />
        <Link className="btn btn-primary" to="/characters">
          Персонажи
        </Link>
      </Panel>
    );
  }

  const hp = characterHp(character);
  const attributes = objectOf(character.attributes);
  const inventoryCount = state.inventory.length;

  return (
    <Panel title="Герой">
      <div className="hero-card">
        <p className="eyebrow">уровень {characterLevel(character)}</p>
        <h2>{characterName(character)}</h2>
        <p className="muted">
          {characterSpecies(character)} · {characterClass(character)}
        </p>
        <div className="metric-grid">
          <Metric label="HP" value={`${hp.current}/${hp.max}`} />
          <Metric label="AC" value={characterAc(character)} />
          <Metric label="Сила" value={numberOf(attributes?.strength, 10)} />
          <Metric label="Ловк." value={numberOf(attributes?.dexterity, 10)} />
          <Metric label="Инв." value={inventoryCount} />
          <Metric label="XP" value={numberOf(objectOf(character.progression)?.experience, 0)} />
        </div>
      </div>
    </Panel>
  );
}

function DicePanel({
  dice,
  state,
  canAct,
  resolving,
  onRoll,
  onResolve,
}: {
  dice: number | null;
  state: PlayStateResponse;
  canAct: boolean;
  resolving: boolean;
  onRoll: () => number;
  onResolve: (requestId: string) => void;
}) {
  const request = state.mechanicRequests[0];
  const requestId = idOf(request?.id);

  return (
    <Panel title="Стол и кубики">
      <div className="dice-tray">
        <button type="button" className="dice-button" onClick={onRoll}>
          <Dice5 size={32} />
          <strong>{dice ?? 'd20'}</strong>
        </button>
        <p className="muted">Кидай кубик для себя или закрывай проверку, когда мастер попросит бросок.</p>
      </div>
      {request ? (
        <div className="roll-request">
          <strong>{firstText(request, ['type', 'тип'], 'Проверка')}</strong>
          <span>{describeRequest(request)}</span>
          <Button disabled={!canAct || !requestId || resolving} onClick={() => requestId && onResolve(requestId)}>
            <Sparkles size={18} /> Бросить и продолжить
          </Button>
        </div>
      ) : (
        <p className="muted">Активных проверок нет.</p>
      )}
    </Panel>
  );
}

function QuickActions({ disabled, onPick }: { disabled: boolean; onPick: (text: string) => void }) {
  const actions = ['Осмотреться', 'Поговорить с ближайшим NPC', 'Искать следы', 'Проверить инвентарь', 'Двигаться осторожно'];
  return (
    <Panel title="Быстрые действия">
      <div className="quick-actions">
        {actions.map((action) => (
          <button className="quick-action" disabled={disabled} key={action} type="button" onClick={() => onPick(action)}>
            {action}
          </button>
        ))}
      </div>
    </Panel>
  );
}

function WorldStatus({ state }: { state: PlayStateResponse }) {
  const time = objectOf(state.time);
  const pending = state.pendingChanges.length;
  return (
    <Panel title="Состояние">
      <div className="metric-grid">
        <Metric label="Режим" value={state.mode} />
        <Metric label="Pending" value={pending} />
        <Metric label="День" value={numberOf(time?.day, 1)} />
        <Metric label="Час" value={numberOf(time?.hour, 0)} />
      </div>
      {pending > 0 ? (
        <p className="muted">
          Мастер подготовил изменения состояния. Безопасные изменения применяются автоматически, технические детали скрыты из игрового режима.
        </p>
      ) : null}
    </Panel>
  );
}

function normalizePlayState(state: PlayStateResponse | undefined): PlayStateResponse | undefined {
  if (!state || typeof state !== 'object') {
    return undefined;
  }

  return {
    ...state,
    mode: textOf(state.mode, 'narration'),
    characters: arrayOfObjects(state.characters),
    recentTurns: arrayOfObjects(state.recentTurns),
    pendingChanges: arrayOfObjects(state.pendingChanges),
    mechanicRequests: arrayOfObjects(state.mechanicRequests),
    inventory: arrayOfObjects(state.inventory),
    appliedChanges: Array.isArray(state.appliedChanges) ? state.appliedChanges : [],
    skippedChanges: Array.isArray(state.skippedChanges) ? state.skippedChanges : [],
    failedChanges: Array.isArray(state.failedChanges) ? state.failedChanges : [],
    monsters: arrayOfObjects(state.monsters),
    loot: arrayOfObjects(state.loot),
    rewards: arrayOfObjects(state.rewards),
    activeConditions: arrayOfObjects(state.activeConditions),
    characterStates: arrayOfObjects(state.characterStates),
    generatedAt: textOf(state.generatedAt, new Date().toISOString()),
    permissions: state.permissions ?? defaultPermissions,
    currentPartyMember: state.currentPartyMember ?? null,
  };
}

const defaultPermissions: PlayPermissions = {
  canRead: true,
  canPlay: true,
  canManage: false,
  canViewSecrets: false,
  canControlSelectedCharacter: true,
};

function BackButton({ onClick }: { onClick: () => void }) {
  return (
    <Button variant="ghost" onClick={onClick}>
      <ArrowLeft size={18} /> Меню
    </Button>
  );
}

function Metric({ label, value }: { label: string; value: string | number }) {
  return (
    <div className="metric">
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}

function realtimeLabel(status: 'online' | 'reconnecting' | 'polling') {
  if (status === 'online') return 'online';
  if (status === 'reconnecting') return 'reconnecting';
  return 'polling';
}

function describeRequest(request: JsonObject) {
  const payload = objectOf(request.payload ?? request.данные);
  const ability = firstText(payload, ['ability', 'характеристика'], '');
  const dc = textOf(payload?.difficultyClass ?? payload?.сложность, '');
  const reason = firstText(payload, ['reason', 'причина'], '');
  return [ability && `характеристика: ${ability}`, dc && `сложность: ${dc}`, reason].filter(Boolean).join(' · ') || 'Мастер просит бросок.';
}
