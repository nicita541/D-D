import { useMemo, useState, type FormEvent } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate, useParams } from 'react-router-dom';
import {
  ArrowLeft,
  Backpack,
  Coins,
  Dice5,
  Footprints,
  RotateCw,
  ScrollText,
  Send,
  Shield,
  Sparkles,
  Swords,
} from 'lucide-react';
import { combatApi, inventoryApi, lootApi, playApi, travelApi, charactersApi } from '../shared/api/endpoints';
import type { JsonObject, PlayStateResponse } from '../shared/api/types';
import { arrayOfObjects, boolOf, firstText, idOf, numberOf, objectOf, textOf } from '../shared/api/json';
import { AppShell, Button, EmptyState, ErrorState, LoadingState, Panel } from '../shared/components/ui';
import { getErrorMessage } from '../shared/api/errors';

export function PlayPage() {
  const { gameStateId = '' } = useParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [selectedCharacterId, setSelectedCharacterId] = useState<string>('');
  const status = useQuery({
    queryKey: ['play-status', gameStateId],
    queryFn: () => playApi.status(gameStateId),
    refetchInterval: 15000,
    enabled: Boolean(gameStateId),
  });

  const selectedCharacter = useMemo(() => {
    const characters = status.data?.characters ?? [];
    if (selectedCharacterId) {
      return characters.find((character) => character.id === selectedCharacterId) ?? characters[0];
    }
    return characters[0];
  }, [selectedCharacterId, status.data?.characters]);

  const activeCharacterId = selectedCharacterId || idOf(selectedCharacter?.id) || '';

  function refresh() {
    void queryClient.invalidateQueries({ queryKey: ['play-status', gameStateId] });
  }

  if (status.isLoading) {
    return (
      <AppShell title="Игровой стол" actions={<BackButton onClick={() => navigate('/games')} />}>
        <LoadingState text="Загружаем состояние игры..." />
      </AppShell>
    );
  }

  if (status.error) {
    return (
      <AppShell title="Игровой стол" actions={<BackButton onClick={() => navigate('/games')} />}>
        <ErrorState error={status.error} />
      </AppShell>
    );
  }

  const state = status.data;
  if (!state || state.characters.length === 0) {
    return (
      <AppShell
        title="Нужен персонаж"
        subtitle="Создай героя перед началом одиночной игры."
        actions={<BackButton onClick={() => navigate('/games')} />}
      >
        <Panel>
          <EmptyState title="Персонажей нет" text="Setup создаст персонажа и стартовую сцену." />
          <Link className="btn btn-primary" to={`/games/${gameStateId}/setup`}>
            Перейти к setup
          </Link>
        </Panel>
      </AppShell>
    );
  }

  return (
    <AppShell
      title="Игровой стол"
      subtitle={`Режим: ${modeLabel(state.mode)} · обновлено ${new Date(state.generatedAt).toLocaleTimeString()}`}
      actions={
        <>
          <Button variant="secondary" onClick={refresh}>
            <RotateCw size={18} /> Обновить
          </Button>
          <BackButton onClick={() => navigate('/games')} />
        </>
      }
    >
      <div className="play-grid">
        <aside className="side-column">
          <CharacterPanel
            state={state}
            selectedCharacterId={activeCharacterId}
            onSelect={(id) => setSelectedCharacterId(id)}
            onChanged={refresh}
          />
          <InventoryPanel gameStateId={gameStateId} characterId={activeCharacterId} state={state} onChanged={refresh} />
          <ProgressionPanel gameStateId={gameStateId} characterId={activeCharacterId} state={state} onChanged={refresh} />
        </aside>

        <section className="main-column">
          <ScenePanel state={state} />
          <ActionPanel gameStateId={gameStateId} characterId={activeCharacterId} state={state} onChanged={refresh} />
          <TurnsPanel state={state} />
        </section>

        <aside className="side-column">
          <MechanicsPanel gameStateId={gameStateId} characterId={activeCharacterId} state={state} onChanged={refresh} />
          <ChangesPanel gameStateId={gameStateId} state={state} onChanged={refresh} />
          <TravelPanel gameStateId={gameStateId} onChanged={refresh} />
          <CombatPanel gameStateId={gameStateId} state={state} onChanged={refresh} />
          <LootPanel gameStateId={gameStateId} characterId={activeCharacterId} state={state} onChanged={refresh} />
        </aside>
      </div>
    </AppShell>
  );
}

function BackButton({ onClick }: { onClick: () => void }) {
  return (
    <Button variant="ghost" onClick={onClick}>
      <ArrowLeft size={18} /> Игры
    </Button>
  );
}

function CharacterPanel({
  state,
  selectedCharacterId,
  onSelect,
  onChanged,
}: {
  state: PlayStateResponse;
  selectedCharacterId: string;
  onSelect: (id: string) => void;
  onChanged: () => void;
}) {
  const character = state.characters.find((item) => item.id === selectedCharacterId) ?? state.characters[0];
  const resources = objectOf(character?.resources ?? character?.ресурсы);
  const progression = objectOf(character?.progression ?? character?.прогресс ?? state.progression);
  const currency = objectOf(state.currency);

  return (
    <Panel title="Персонажи">
      <select value={selectedCharacterId} onChange={(event) => onSelect(event.target.value)}>
        {state.characters.map((item) => (
          <option key={String(item.id)} value={String(item.id)}>
            {firstText(item, ['name', 'имя'], 'Персонаж')}
          </option>
        ))}
      </select>
      <div className="hero-card">
        <h3>{firstText(character, ['name', 'имя'], 'Персонаж')}</h3>
        <p className="muted">
          {firstText(character, ['species', 'вид'], 'вид')} · {firstText(character, ['className', 'class', 'класс'], 'класс')}
        </p>
        <div className="metric-grid">
          <Metric label="HP" value={`${numberOf(resources?.hpCurrent ?? resources?.хпТекущее, 0)} / ${numberOf(resources?.hpMax ?? resources?.хпМаксимум, 0)}`} />
          <Metric label="Уровень" value={numberOf(progression?.level ?? progression?.уровень, 1)} />
          <Metric label="XP" value={numberOf(progression?.experience ?? progression?.опыт, 0)} />
          <Metric label="Золото" value={numberOf(currency?.gold ?? currency?.золотые, 0)} />
        </div>
        {boolOf(progression?.levelUpAvailable, false) ? <LevelUpButton characterId={String(character.id)} onChanged={onChanged} /> : null}
      </div>
    </Panel>
  );
}

function LevelUpButton({ characterId, onChanged }: { characterId: string; onChanged: () => void }) {
  const { gameStateId = '' } = useParams();
  const levelUp = useMutation({
    mutationFn: () => charactersApi.levelUp(gameStateId, characterId),
    onSuccess: onChanged,
  });
  return (
    <Button onClick={() => levelUp.mutate()} disabled={levelUp.isPending}>
      <Sparkles size={18} /> Повысить уровень
    </Button>
  );
}

function ScenePanel({ state }: { state: PlayStateResponse }) {
  const scene = state.scene;
  return (
    <Panel title={scene?.title ?? 'Текущая сцена'} className="scene-panel">
      <p className="scene-summary">{scene?.summary ?? 'Сцена появится после bootstrap или первого действия.'}</p>
      <div className="scene-facts">
        <span>Цель: {scene?.currentObjective ?? 'не задана'}</span>
        <span>Угроза: {scene?.currentThreat ?? 'неизвестна'}</span>
      </div>
      {state.masterAnswer ? <blockquote>{state.masterAnswer}</blockquote> : null}
    </Panel>
  );
}

function ActionPanel({
  gameStateId,
  characterId,
  state,
  onChanged,
}: {
  gameStateId: string;
  characterId: string;
  state: PlayStateResponse;
  onChanged: () => void;
}) {
  const [message, setMessage] = useState('');
  const action = useMutation({
    mutationFn: () => playApi.act(gameStateId, { message, characterId, autoApplySafeChanges: true }),
    onSuccess: () => {
      setMessage('');
      onChanged();
    },
  });

  function submit(event: FormEvent) {
    event.preventDefault();
    if (!message.trim() || state.mode === 'awaiting_roll') return;
    action.mutate();
  }

  return (
    <Panel title="Действие игрока">
      {state.mode === 'awaiting_roll' ? (
        <div className="notice">Сначала нужно закрыть pending бросок. Новые действия не отправляются, пока игра ждёт проверку.</div>
      ) : null}
      <form className="action-form" onSubmit={submit}>
        <textarea
          value={message}
          onChange={(event) => setMessage(event.target.value)}
          placeholder="Я осматриваюсь вокруг и ищу следы..."
          maxLength={4000}
        />
        <Button type="submit" disabled={action.isPending || !message.trim() || state.mode === 'awaiting_roll'}>
          <Send size={18} /> {action.isPending ? 'Отправляем...' : 'Действовать'}
        </Button>
      </form>
      {action.error ? <p className="form-error">{getErrorMessage(action.error)}</p> : null}
    </Panel>
  );
}

function MechanicsPanel({
  gameStateId,
  characterId,
  state,
  onChanged,
}: {
  gameStateId: string;
  characterId: string;
  state: PlayStateResponse;
  onChanged: () => void;
}) {
  const resolve = useMutation({
    mutationFn: (requestId: string) => playApi.resolveAndContinue(gameStateId, requestId, characterId),
    onSuccess: onChanged,
  });

  return (
    <Panel title="Броски" actions={<Dice5 size={18} />}>
      {state.mechanicRequests.length === 0 ? <EmptyState title="Нет ожидающих бросков" /> : null}
      <div className="list-stack">
        {state.mechanicRequests.map((request) => (
          <div className="mini-card" key={String(request.id)}>
            <strong>{textOf(request.requestType ?? request.type, 'проверка')}</strong>
            <small>{describePayload(objectOf(request.payload))}</small>
            <Button onClick={() => resolve.mutate(String(request.id))} disabled={resolve.isPending || !characterId}>
              Бросить и продолжить
            </Button>
          </div>
        ))}
      </div>
      {resolve.error ? <p className="form-error">{getErrorMessage(resolve.error)}</p> : null}
    </Panel>
  );
}

function ChangesPanel({ gameStateId, state, onChanged }: { gameStateId: string; state: PlayStateResponse; onChanged: () => void }) {
  const applySafe = useMutation({ mutationFn: () => playApi.applySafeChanges(gameStateId), onSuccess: onChanged });
  const apply = useMutation({ mutationFn: (id: string) => playApi.applyChange(gameStateId, id), onSuccess: onChanged });
  const reject = useMutation({ mutationFn: (id: string) => playApi.rejectChange(gameStateId, id, 'Отклонено игроком.'), onSuccess: onChanged });

  return (
    <Panel
      title="Изменения"
      actions={
        <Button variant="secondary" onClick={() => applySafe.mutate()} disabled={applySafe.isPending || state.pendingChanges.length === 0}>
          Auto safe
        </Button>
      }
    >
      {state.pendingChanges.length === 0 ? <EmptyState title="Нет pending changes" /> : null}
      <div className="list-stack">
        {state.pendingChanges.map((change) => (
          <div className="mini-card" key={String(change.id)}>
            <strong>{textOf(change.operation, 'operation')}</strong>
            <small>{truncate(JSON.stringify(change.payload ?? {}), 120)}</small>
            <div className="button-row">
              <Button variant="secondary" onClick={() => apply.mutate(String(change.id))}>
                Применить
              </Button>
              <Button variant="danger" onClick={() => reject.mutate(String(change.id))}>
                Отклонить
              </Button>
            </div>
          </div>
        ))}
      </div>
      {[applySafe.error, apply.error, reject.error].filter(Boolean).map((error, index) => (
        <p className="form-error" key={index}>
          {getErrorMessage(error)}
        </p>
      ))}
    </Panel>
  );
}

function InventoryPanel({
  gameStateId,
  characterId,
  state,
  onChanged,
}: {
  gameStateId: string;
  characterId: string;
  state: PlayStateResponse;
  onChanged: () => void;
}) {
  const [name, setName] = useState('Дорожный паёк');
  const [itemType, setItemType] = useState('consumable');
  const create = useMutation({
    mutationFn: () =>
      inventoryApi.create(gameStateId, characterId, {
        name,
        description: 'Создано из frontend.',
        itemType,
        quantity: 1,
        weight: 0,
        slot: itemType === 'weapon' ? 'main_hand' : null,
        properties: itemType === 'consumable' ? { effect: 'heal', amount: 1 } : {},
      }),
    onSuccess: onChanged,
  });
  const itemAction = useMutation({
    mutationFn: ({ action, itemId }: { action: 'equip' | 'unequip' | 'use' | 'delete'; itemId: string }) => {
      if (action === 'equip') return inventoryApi.equip(gameStateId, characterId, itemId);
      if (action === 'unequip') return inventoryApi.unequip(gameStateId, characterId, itemId);
      if (action === 'use') return inventoryApi.use(gameStateId, characterId, itemId);
      return inventoryApi.delete(gameStateId, characterId, itemId);
    },
    onSuccess: onChanged,
  });

  return (
    <Panel title="Инвентарь" actions={<Backpack size={18} />}>
      <form className="inline-form" onSubmit={(event) => event.preventDefault()}>
        <input value={name} onChange={(event) => setName(event.target.value)} />
        <select value={itemType} onChange={(event) => setItemType(event.target.value)}>
          <option value="consumable">consumable</option>
          <option value="weapon">weapon</option>
          <option value="armor">armor</option>
          <option value="misc">misc</option>
        </select>
        <Button onClick={() => create.mutate()} disabled={create.isPending || !characterId}>
          Добавить
        </Button>
      </form>
      {state.inventory.length === 0 ? <EmptyState title="Инвентарь пуст" /> : null}
      <div className="list-stack">
        {state.inventory.map((item) => {
          const id = String(item.id);
          return (
            <div className="mini-card" key={id}>
              <strong>{firstText(item, ['name', 'название'], 'Предмет')}</strong>
              <small>
                {textOf(item.itemType ?? item.item_type ?? item.тип, 'item')} · x{numberOf(item.quantity ?? item.количество, 1)}
              </small>
              <div className="button-row">
                <Button variant="secondary" onClick={() => itemAction.mutate({ action: 'equip', itemId: id })}>
                  Надеть
                </Button>
                <Button variant="secondary" onClick={() => itemAction.mutate({ action: 'unequip', itemId: id })}>
                  Снять
                </Button>
                <Button variant="secondary" onClick={() => itemAction.mutate({ action: 'use', itemId: id })}>
                  Использовать
                </Button>
                <Button variant="danger" onClick={() => itemAction.mutate({ action: 'delete', itemId: id })}>
                  Удалить
                </Button>
              </div>
            </div>
          );
        })}
      </div>
      {[create.error, itemAction.error].filter(Boolean).map((error, index) => (
        <p className="form-error" key={index}>
          {getErrorMessage(error)}
        </p>
      ))}
    </Panel>
  );
}

function TravelPanel({ gameStateId, onChanged }: { gameStateId: string; onChanged: () => void }) {
  const options = useQuery({ queryKey: ['travel-options', gameStateId], queryFn: () => travelApi.options(gameStateId) });
  const travel = useMutation({
    mutationFn: ({ targetLocationId, exitId }: { targetLocationId: string; exitId?: string }) => travelApi.travel(gameStateId, targetLocationId, exitId),
    onSuccess: onChanged,
  });

  return (
    <Panel title="Путешествие" actions={<Footprints size={18} />}>
      {options.isLoading ? <LoadingState text="Ищем маршруты..." /> : null}
      {options.data?.length === 0 ? <EmptyState title="Нет доступных переходов" /> : null}
      <div className="list-stack">
        {options.data?.map((option, index) => {
          const target = idOf(option.targetLocationId ?? option.locationId ?? option.id);
          const exitId = idOf(option.exitId);
          return (
            <div className="mini-card" key={`${target ?? index}-${exitId ?? 'direct'}`}>
              <strong>{firstText(option, ['targetLocationName', 'name', 'название'], 'Локация')}</strong>
              <small>{firstText(option, ['direction', 'description', 'описание'], exitId ? 'Переход' : 'Прямой переход')}</small>
              <Button variant="secondary" disabled={!target || travel.isPending} onClick={() => target && travel.mutate({ targetLocationId: target, exitId })}>
                Перейти
              </Button>
            </div>
          );
        })}
      </div>
      {travel.error ? <p className="form-error">{getErrorMessage(travel.error)}</p> : null}
    </Panel>
  );
}

function CombatPanel({ gameStateId, state, onChanged }: { gameStateId: string; state: PlayStateResponse; onChanged: () => void }) {
  const start = useMutation({ mutationFn: () => combatApi.start(gameStateId), onSuccess: onChanged });
  const end = useMutation({ mutationFn: () => combatApi.end(gameStateId), onSuccess: onChanged });
  const resolve = useMutation({ mutationFn: () => combatApi.resolveOutcome(gameStateId), onSuccess: onChanged });
  const participants = arrayOfObjects(state.combat?.participants);

  return (
    <Panel title="Бой" actions={<Swords size={18} />}>
      {!state.combat ? <EmptyState title="Активного боя нет" /> : null}
      {state.combat ? (
        <>
          <div className="metric-grid">
            <Metric label="Раунд" value={numberOf(state.combat.roundNumber ?? state.combat.round_number, 1)} />
            <Metric label="Участники" value={participants.length} />
          </div>
          <div className="list-stack">
            {participants.slice(0, 4).map((participant) => (
              <div className="mini-card" key={String(participant.id)}>
                <strong>{firstText(participant, ['name', 'имя'], 'Участник')}</strong>
                <small>
                  HP {numberOf(participant.hpCurrent ?? participant.hp_current, 0)} / {numberOf(participant.hpMax ?? participant.hp_max, 0)}
                </small>
              </div>
            ))}
          </div>
        </>
      ) : null}
      <div className="button-row">
        <Button variant="secondary" onClick={() => start.mutate()} disabled={start.isPending}>
          Начать
        </Button>
        <Button variant="secondary" onClick={() => resolve.mutate()} disabled={resolve.isPending}>
          Исход
        </Button>
        <Button variant="danger" onClick={() => end.mutate()} disabled={end.isPending}>
          Завершить
        </Button>
      </div>
      {[start.error, end.error, resolve.error].filter(Boolean).map((error, index) => (
        <p className="form-error" key={index}>
          {getErrorMessage(error)}
        </p>
      ))}
    </Panel>
  );
}

function ProgressionPanel({
  gameStateId,
  characterId,
  state,
  onChanged,
}: {
  gameStateId: string;
  characterId: string;
  state: PlayStateResponse;
  onChanged: () => void;
}) {
  const [amount, setAmount] = useState(50);
  const xp = useMutation({
    mutationFn: () => charactersApi.addXp(gameStateId, characterId, amount, 'Начислено через frontend.'),
    onSuccess: onChanged,
  });
  const progression = objectOf(state.progression);

  return (
    <Panel title="Прогресс" actions={<Shield size={18} />}>
      <div className="metric-grid">
        <Metric label="Level" value={numberOf(progression?.level, 1)} />
        <Metric label="XP" value={numberOf(progression?.experience, 0)} />
        <Metric label="Next" value={numberOf(progression?.experienceToNextLevel, 300)} />
        <Metric label="Prof" value={numberOf(progression?.proficiencyBonus, 2)} />
      </div>
      <div className="inline-form">
        <input type="number" value={amount} onChange={(event) => setAmount(Number(event.target.value))} />
        <Button variant="secondary" onClick={() => xp.mutate()} disabled={!characterId || xp.isPending}>
          +XP
        </Button>
      </div>
      {xp.error ? <p className="form-error">{getErrorMessage(xp.error)}</p> : null}
    </Panel>
  );
}

function LootPanel({
  gameStateId,
  characterId,
  state,
  onChanged,
}: {
  gameStateId: string;
  characterId: string;
  state: PlayStateResponse;
  onChanged: () => void;
}) {
  const claim = useMutation({ mutationFn: (id: string) => lootApi.claim(gameStateId, id, characterId), onSuccess: onChanged });
  const available = state.loot.filter((item) => textOf(item.status, 'available') === 'available');

  return (
    <Panel title="Награды" actions={<Coins size={18} />}>
      {available.length === 0 ? <EmptyState title="Добычи нет" /> : null}
      {available.map((loot) => (
        <div className="mini-card" key={String(loot.id)}>
          <strong>{firstText(loot, ['name', 'название'], 'Добыча')}</strong>
          <small>{truncate(JSON.stringify(loot), 100)}</small>
          <Button variant="secondary" onClick={() => claim.mutate(String(loot.id))} disabled={claim.isPending || !characterId}>
            Забрать
          </Button>
        </div>
      ))}
      {claim.error ? <p className="form-error">{getErrorMessage(claim.error)}</p> : null}
    </Panel>
  );
}

function TurnsPanel({ state }: { state: PlayStateResponse }) {
  return (
    <Panel title="Журнал" actions={<ScrollText size={18} />}>
      {state.recentTurns.length === 0 ? <EmptyState title="Ходов пока нет" /> : null}
      <div className="list-stack">
        {state.recentTurns.slice(0, 8).map((turn, index) => (
          <div className="mini-card" key={String(turn.id ?? index)}>
            <strong>Ход {textOf(turn.turnNumber ?? turn.turn_number ?? index + 1)}</strong>
            <small>{truncate(textOf(turn.masterAnswer ?? turn.master_answer ?? turn.playerMessage ?? turn.player_message, 'Событие'), 220)}</small>
          </div>
        ))}
      </div>
    </Panel>
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

function modeLabel(mode: string) {
  const labels: Record<string, string> = {
    narration: 'повествование',
    awaiting_roll: 'нужен бросок',
    awaiting_change_confirmation: 'подтверждение изменений',
    combat: 'бой',
    travel: 'переход',
  };
  return labels[mode] ?? mode;
}

function describePayload(payload: JsonObject | null) {
  if (!payload) return 'Нет деталей';
  const ability = textOf(payload.ability ?? payload.характеристика, '');
  const dc = textOf(payload.difficultyClass ?? payload.сложность, '');
  const reason = textOf(payload.reason ?? payload.причина, '');
  return [ability && `характеристика: ${ability}`, dc && `сложность: ${dc}`, reason].filter(Boolean).join(' · ') || 'Проверка';
}

function truncate(value: string, max: number) {
  return value.length <= max ? value : `${value.slice(0, max)}...`;
}
