import { useMemo, useState, type FormEvent } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate, useParams } from 'react-router-dom';
import {
  ArrowLeft,
  Backpack,
  Camera,
  Clock,
  Coins,
  Dice5,
  Footprints,
  Link2,
  RotateCw,
  ScrollText,
  Send,
  Settings,
  Shield,
  Sparkles,
  Swords,
  Undo2,
  Users,
} from 'lucide-react';
import { combatApi, inventoryApi, lootApi, partyApi, playApi, snapshotsApi, travelApi, charactersApi, restApi, timeApi } from '../shared/api/endpoints';
import type { JsonObject, PlayPermissions, PlayStateResponse, SnapshotDto } from '../shared/api/types';
import { arrayOfObjects, boolOf, firstText, idOf, numberOf, objectOf, textOf } from '../shared/api/json';
import { AppShell, Button, ConfirmButton, EmptyState, ErrorState, LoadingState, Panel } from '../shared/components/ui';
import { getErrorMessage } from '../shared/api/errors';
import { modeInfo, operationDanger, operationLabel } from './labels';
import { queryKeys } from '../shared/api/query-keys';
import { normalizeTravelOptions } from './travel';
import { useGameRealtime } from './useGameRealtime';

export function PlayPage() {
  const { gameStateId = '' } = useParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const realtimeStatus = useGameRealtime(gameStateId);
  const selectedCharacterStorageKey = `dnd.selectedCharacter.${gameStateId}`;
  const [selectedCharacterId, setSelectedCharacterId] = useState<string>(() =>
    gameStateId ? window.localStorage.getItem(`dnd.selectedCharacter.${gameStateId}`) ?? '' : '',
  );
  const status = useQuery({
    queryKey: queryKeys.play(gameStateId),
    queryFn: () => playApi.status(gameStateId),
    refetchInterval: 10000,
    enabled: Boolean(gameStateId),
  });
  const party = useQuery({
    queryKey: queryKeys.party(gameStateId),
    queryFn: () => partyApi.get(gameStateId),
    enabled: Boolean(gameStateId),
  });
  const snapshots = useQuery({
    queryKey: queryKeys.snapshots(gameStateId),
    queryFn: () => snapshotsApi.list(gameStateId),
    enabled: Boolean(gameStateId),
  });
  const state = useMemo(() => normalizePlayState(status.data), [status.data]);
  const permissions = state?.permissions ?? defaultPermissions;
  const memberCharacterId = idOf(state?.currentPartyMember?.characterId) || '';

  const selectedCharacter = useMemo(() => {
    const characters = state?.characters ?? [];
    if (!permissions.canManage && memberCharacterId) {
      return characters.find((character) => character.id === memberCharacterId) ?? characters[0];
    }
    if (selectedCharacterId) {
      return characters.find((character) => character.id === selectedCharacterId) ?? characters[0];
    }
    return characters[0];
  }, [memberCharacterId, permissions.canManage, selectedCharacterId, state?.characters]);

  const activeCharacterId = permissions.canManage
    ? selectedCharacterId || idOf(selectedCharacter?.id) || ''
    : memberCharacterId || idOf(selectedCharacter?.id) || '';
  const canControlActiveCharacter = permissions.canManage || (Boolean(activeCharacterId) && memberCharacterId === activeCharacterId);

  function selectCharacter(id: string) {
    if (!permissions.canManage && memberCharacterId && id !== memberCharacterId) {
      return;
    }

    setSelectedCharacterId(id);
    window.localStorage.setItem(selectedCharacterStorageKey, id);
  }

  function refresh() {
    void queryClient.invalidateQueries({ queryKey: queryKeys.play(gameStateId) });
    void queryClient.invalidateQueries({ queryKey: queryKeys.party(gameStateId) });
    void queryClient.invalidateQueries({ queryKey: queryKeys.snapshots(gameStateId) });
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

  if (!state || (permissions.canPlay && !permissions.canManage && !memberCharacterId)) {
    return (
      <AppShell
        title="Нужен персонаж"
        subtitle="Создай героя и привяжи его к своему участнику партии."
        actions={<BackButton onClick={() => navigate('/games')} />}
      >
        <Panel>
          <EmptyState title="Персонаж не назначен" text="Setup создаст героя и backend назначит его твоему party member." />
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
      subtitle={`${modeInfo(state.mode).label}: ${modeInfo(state.mode).text} · realtime: ${realtimeLabel(realtimeStatus)} · обновлено ${new Date(state.generatedAt).toLocaleTimeString()}`}
      actions={
        <>
          <Button variant="secondary" onClick={refresh}>
            <RotateCw size={18} /> Обновить
          </Button>
          {permissions.canManage ? (
          <Button variant="secondary" onClick={() => navigate(`/games/${gameStateId}/manage/characters`)}>
            <Settings size={18} /> Управление
          </Button>
          ) : null}
          {permissions.canManage ? (
            <Button variant="secondary" onClick={() => navigate(`/games/${gameStateId}/invite`)}>
              <Link2 size={18} /> Invite
            </Button>
          ) : null}
          <BackButton onClick={() => navigate('/games')} />
        </>
      }
    >
      <div className="play-grid">
        <aside className="side-column">
          <CharacterPanel
            state={state}
            selectedCharacterId={activeCharacterId}
            onSelect={selectCharacter}
            onChanged={refresh}
            locked={!permissions.canManage}
          />
          <PartyPanel
            gameStateId={gameStateId}
            party={objectOf(party.data)}
            characters={state.characters}
            permissions={permissions}
            currentMemberId={idOf(state.currentPartyMember?.id)}
            onChanged={refresh}
          />
          <InventoryPanel gameStateId={gameStateId} characterId={activeCharacterId} state={state} onChanged={refresh} disabled={!canControlActiveCharacter} />
          <ProgressionPanel state={state} />
          <RestTimePanel gameStateId={gameStateId} characterId={activeCharacterId} state={state} onChanged={refresh} disabled={!canControlActiveCharacter} />
        </aside>

        <section className="main-column">
          <ScenePanel state={state} />
          <ActionPanel gameStateId={gameStateId} characterId={activeCharacterId} state={state} onChanged={refresh} permissions={permissions} canControlActiveCharacter={canControlActiveCharacter} />
          <TurnsPanel state={state} />
        </section>

        <aside className="side-column">
          <MechanicsPanel gameStateId={gameStateId} characterId={activeCharacterId} state={state} onChanged={refresh} disabled={!canControlActiveCharacter} />
          <ChangesPanel gameStateId={gameStateId} state={state} onChanged={refresh} canManage={permissions.canManage} />
          <TravelPanel gameStateId={gameStateId} onChanged={refresh} disabled={!permissions.canPlay} />
          <CombatPanel gameStateId={gameStateId} state={state} onChanged={refresh} activeCharacterId={activeCharacterId} permissions={permissions} />
          <LootPanel gameStateId={gameStateId} characterId={activeCharacterId} state={state} onChanged={refresh} disabled={!canControlActiveCharacter} />
          <SnapshotsPanel gameStateId={gameStateId} snapshots={snapshots.data ?? []} canManage={permissions.canManage} onChanged={refresh} />
        </aside>
      </div>
    </AppShell>
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

function realtimeLabel(status: 'online' | 'reconnecting' | 'polling') {
  if (status === 'online') return 'online';
  if (status === 'reconnecting') return 'reconnecting';
  return 'polling';
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
  locked,
}: {
  state: PlayStateResponse;
  selectedCharacterId: string;
  onSelect: (id: string) => void;
  onChanged: () => void;
  locked: boolean;
}) {
  const character = state.characters.find((item) => item.id === selectedCharacterId) ?? state.characters[0];
  const resources = objectOf(character?.resources ?? character?.ресурсы);
  const progression = objectOf(character?.progression ?? character?.прогресс ?? state.progression);
  const currency = objectOf(state.currency);
  const selectedState = state.characterStates.find((item) => String(item.characterId ?? item.id) === selectedCharacterId);
  const selectedConditions = state.activeConditions.filter((condition) => String(condition.characterId ?? condition.playerId ?? condition.ownerId ?? '') === selectedCharacterId);

  return (
    <Panel title="Персонажи">
      <select value={selectedCharacterId} onChange={(event) => onSelect(event.target.value)} disabled={locked}>
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
          <Metric label="Mana" value={`${numberOf(resources?.manaCurrent ?? resources?.манаТекущая, 0)} / ${numberOf(resources?.manaMax ?? resources?.манаМаксимум, 0)}`} />
          <Metric label="AP" value={`${numberOf(resources?.actionPointsCurrent ?? resources?.очкиДействийТекущие, 0)} / ${numberOf(resources?.actionPointsMax ?? resources?.очкиДействийМаксимум, 0)}`} />
          <Metric label="Уровень" value={numberOf(progression?.level ?? progression?.уровень, 1)} />
          <Metric label="XP" value={numberOf(progression?.experience ?? progression?.опыт, 0)} />
          <Metric label="Next" value={numberOf(progression?.nextLevelThreshold ?? progression?.experienceToNextLevel, 300)} />
          <Metric label="Prof" value={numberOf(progression?.proficiencyBonus, 2)} />
          <Metric label="Золото" value={numberOf(currency?.gold ?? currency?.золотые, 0)} />
        </div>
        <div className="condition-list">
          {boolOf(selectedState?.unconscious, false) ? <span className="badge badge-danger">без сознания</span> : null}
          {boolOf(selectedState?.dead, false) ? <span className="badge badge-danger">мёртв</span> : null}
          {selectedConditions.map((condition, index) => (
            <span className="badge" key={String(condition.id ?? index)}>
              {firstText(condition, ['name', 'название'], 'состояние')}
            </span>
          ))}
        </div>
        {boolOf(progression?.levelUpAvailable, false) ? <LevelUpButton characterId={String(character.id)} onChanged={onChanged} /> : null}
      </div>
    </Panel>
  );
}

function PartyPanel({
  gameStateId,
  party,
  characters,
  permissions,
  currentMemberId,
  onChanged,
}: {
  gameStateId: string;
  party: JsonObject | null;
  characters: JsonObject[];
  permissions: PlayPermissions;
  currentMemberId?: string;
  onChanged: () => void;
}) {
  const members = arrayOfObjects(party?.участники ?? party?.members);
  const updateRole = useMutation({
    mutationFn: ({ memberId, role }: { memberId: string; role: string }) => partyApi.updateMember(gameStateId, memberId, { role }),
    onSuccess: onChanged,
  });
  const assignCharacter = useMutation({
    mutationFn: ({ memberId, characterId }: { memberId: string; characterId: string }) =>
      memberId === currentMemberId && !permissions.canManage
        ? partyApi.assignMyCharacter(gameStateId, characterId)
        : partyApi.assignMemberCharacter(gameStateId, memberId, characterId),
    onSuccess: onChanged,
  });
  const remove = useMutation({
    mutationFn: (memberId: string) => partyApi.removeMember(gameStateId, memberId),
    onSuccess: onChanged,
  });

  return (
    <Panel title="Партия" actions={<Users size={18} />}>
      {members.length === 0 ? <EmptyState title="Партия пока пуста" /> : null}
      <div className="list-stack">
        {members.map((member) => {
          const memberId = String(member.id);
          const role = textOf(member.роль ?? member.role, 'player');
          const characterId = idOf(member.characterId);
          const character = characters.find((item) => String(item.id) === characterId);
          const isMe = memberId === currentMemberId;
          return (
            <div className="mini-card" key={memberId}>
              <strong>
                {firstText(member, ['отображаемоеИмя', 'displayName'], isMe ? 'Вы' : 'Участник')} {isMe ? '· вы' : ''}
              </strong>
              <small>
                <span className="badge">{role}</span> персонаж:{' '}
                {character ? firstText(character, ['name', 'имя'], 'Персонаж') : characterId || 'не назначен'}
              </small>
              {permissions.canManage ? (
                <div className="button-row">
                  <select value={role} onChange={(event) => updateRole.mutate({ memberId, role: event.target.value })}>
                    <option value="host">host</option>
                    <option value="player">player</option>
                    <option value="observer">observer</option>
                    <option value="gm">gm</option>
                  </select>
                  <select
                    value={characterId}
                    onChange={(event) => event.target.value && assignCharacter.mutate({ memberId, characterId: event.target.value })}
                  >
                    <option value="">Без персонажа</option>
                    {characters.map((item) => (
                      <option key={String(item.id)} value={String(item.id)}>
                        {firstText(item, ['name', 'имя'], 'Персонаж')}
                      </option>
                    ))}
                  </select>
                  <ConfirmButton variant="danger" confirmText="Удалить участника из партии?" onConfirm={() => remove.mutate(memberId)}>
                    Кик
                  </ConfirmButton>
                </div>
              ) : isMe && permissions.canPlay && !characterId ? (
                <div className="button-row">
                  <select onChange={(event) => event.target.value && assignCharacter.mutate({ memberId, characterId: event.target.value })}>
                    <option value="">Назначить своего героя</option>
                    {characters.map((item) => (
                      <option key={String(item.id)} value={String(item.id)}>
                        {firstText(item, ['name', 'имя'], 'Персонаж')}
                      </option>
                    ))}
                  </select>
                </div>
              ) : null}
            </div>
          );
        })}
      </div>
      {[updateRole.error, assignCharacter.error, remove.error].filter(Boolean).map((error, index) => (
        <p className="form-error" key={index}>
          {getErrorMessage(error)}
        </p>
      ))}
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
  permissions,
  canControlActiveCharacter,
}: {
  gameStateId: string;
  characterId: string;
  state: PlayStateResponse;
  onChanged: () => void;
  permissions: PlayPermissions;
  canControlActiveCharacter: boolean;
}) {
  const [message, setMessage] = useState('');
  const action = useMutation({
    mutationFn: () => playApi.act(gameStateId, { message, characterId, autoApplySafeChanges: true }),
    onSuccess: () => {
      setMessage('');
      onChanged();
    },
  });
  const continueScene = useMutation({
    mutationFn: () => playApi.continue(gameStateId),
    onSuccess: onChanged,
  });

  function submit(event: FormEvent) {
    event.preventDefault();
    if (!message.trim() || state.mode === 'awaiting_roll' || !permissions.canPlay || !canControlActiveCharacter) return;
    action.mutate();
  }

  const actionDisabled = action.isPending || !message.trim() || state.mode === 'awaiting_roll' || !permissions.canPlay || !canControlActiveCharacter;

  return (
    <Panel title="Действие игрока">
      {!permissions.canPlay ? <div className="notice">Вы в режиме наблюдателя. Действия отключены.</div> : null}
      {permissions.canPlay && !canControlActiveCharacter ? <div className="notice">Сначала нужен назначенный персонаж.</div> : null}
      {state.mode === 'awaiting_roll' ? (
        <div className="notice">Сначала нужно закрыть бросок. Новые действия не отправляются, пока игра ждёт проверку.</div>
      ) : null}
      <div className="quick-actions">
        {quickActionTemplates.map((template) => (
          <button
            className="quick-action"
            key={template}
            type="button"
            onClick={() => setMessage((current) => (current.trim() ? `${current.trim()}\n${template}` : template))}
          >
            {template}
          </button>
        ))}
      </div>
      <form className="action-form" onSubmit={submit}>
        <textarea
          value={message}
          onChange={(event) => setMessage(event.target.value)}
          placeholder="Я осматриваюсь вокруг и ищу следы..."
          maxLength={4000}
          disabled={!permissions.canPlay || !canControlActiveCharacter}
        />
        <Button type="submit" disabled={actionDisabled}>
          <Send size={18} /> {action.isPending ? 'Отправляем...' : 'Действовать'}
        </Button>
      </form>
      <Button variant="secondary" disabled={continueScene.isPending || state.mode === 'awaiting_roll' || !permissions.canPlay} onClick={() => continueScene.mutate()}>
        Продолжить сцену
      </Button>
      {[action.error, continueScene.error].filter(Boolean).map((error, index) => <p className="form-error" key={index}>{getErrorMessage(error)}</p>)}
    </Panel>
  );
}

function MechanicsPanel({
  gameStateId,
  characterId,
  state,
  onChanged,
  disabled,
}: {
  gameStateId: string;
  characterId: string;
  state: PlayStateResponse;
  onChanged: () => void;
  disabled: boolean;
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
            <Button onClick={() => resolve.mutate(String(request.id))} disabled={resolve.isPending || !characterId || disabled}>
              Бросить и продолжить
            </Button>
          </div>
        ))}
      </div>
      {resolve.error ? <p className="form-error">{getErrorMessage(resolve.error)}</p> : null}
    </Panel>
  );
}

function ChangesPanel({ gameStateId, state, onChanged, canManage }: { gameStateId: string; state: PlayStateResponse; onChanged: () => void; canManage: boolean }) {
  const applySafe = useMutation({ mutationFn: () => playApi.applySafeChanges(gameStateId), onSuccess: onChanged });
  const apply = useMutation({ mutationFn: (id: string) => playApi.applyChange(gameStateId, id), onSuccess: onChanged });
  const reject = useMutation({ mutationFn: (id: string) => playApi.rejectChange(gameStateId, id, 'Отклонено игроком.'), onSuccess: onChanged });

  return (
    <Panel
      title="Изменения"
      actions={
        canManage ? (
        <Button variant="secondary" onClick={() => applySafe.mutate()} disabled={applySafe.isPending || state.pendingChanges.length === 0}>
          Auto safe
        </Button>
        ) : null
      }
    >
      {state.pendingChanges.length === 0 ? <EmptyState title="Нет pending changes" /> : null}
      <div className="list-stack">
        {state.pendingChanges.map((change) => (
          <div className="mini-card" key={String(change.id)}>
            <div className="change-title">
              <strong>{operationLabel(textOf(change.operation, 'operation'))}</strong>
              <span className={`badge ${operationDanger(textOf(change.operation, '')) ? 'badge-danger' : 'badge-safe'}`}>
                {operationDanger(textOf(change.operation, '')) ? 'опасное' : 'safe'}
              </span>
            </div>
            <small>{payloadSummary(textOf(change.operation, ''), objectOf(change.payload))}</small>
            {canManage ? (
            <div className="button-row">
              {operationDanger(textOf(change.operation, '')) ? (
                <ConfirmButton variant="danger" confirmText="Применить потенциально опасное изменение?" onConfirm={() => apply.mutate(String(change.id))}>
                  Применить
                </ConfirmButton>
              ) : (
                <Button variant="secondary" onClick={() => apply.mutate(String(change.id))}>Применить</Button>
              )}
              <ConfirmButton variant="danger" confirmText="Отклонить изменение?" onConfirm={() => reject.mutate(String(change.id))}>
                Отклонить
              </ConfirmButton>
            </div>
            ) : (
              <small className="muted">Ожидает подтверждения host.</small>
            )}
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
  disabled,
}: {
  gameStateId: string;
  characterId: string;
  state: PlayStateResponse;
  onChanged: () => void;
  disabled: boolean;
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
        <Button onClick={() => create.mutate()} disabled={create.isPending || !characterId || disabled}>
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
                <Button variant="secondary" disabled={disabled} onClick={() => itemAction.mutate({ action: 'equip', itemId: id })}>
                  Надеть
                </Button>
                <Button variant="secondary" disabled={disabled} onClick={() => itemAction.mutate({ action: 'unequip', itemId: id })}>
                  Снять
                </Button>
                <Button variant="secondary" disabled={disabled} onClick={() => itemAction.mutate({ action: 'use', itemId: id })}>
                  Использовать
                </Button>
                <ConfirmButton variant="danger" disabled={disabled} confirmText="Удалить предмет?" onConfirm={() => itemAction.mutate({ action: 'delete', itemId: id })}>
                  Удалить
                </ConfirmButton>
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

function TravelPanel({ gameStateId, onChanged, disabled }: { gameStateId: string; onChanged: () => void; disabled: boolean }) {
  const options = useQuery({ queryKey: queryKeys.world(gameStateId, 'travel-options'), queryFn: () => travelApi.options(gameStateId) });
  const travelOptions = normalizeTravelOptions(options.data);
  const travel = useMutation({
    mutationFn: ({ targetLocationId, exitId }: { targetLocationId: string; exitId?: string }) => travelApi.travel(gameStateId, targetLocationId, exitId),
    onSuccess: onChanged,
  });

  return (
    <Panel title="Путешествие" actions={<Footprints size={18} />}>
      {options.isLoading ? <LoadingState text="Ищем маршруты..." /> : null}
      {!options.isLoading && travelOptions.length === 0 ? <EmptyState title="Нет доступных переходов" /> : null}
      <div className="list-stack">
        {travelOptions.map((option, index) => {
          const targetLocation = objectOf(option.targetLocation);
          const target = idOf(option.targetLocationId ?? targetLocation?.id ?? option.locationId ?? option.id);
          const exitId = idOf(option.exitId);
          return (
            <div className="mini-card" key={`${target ?? index}-${exitId ?? 'direct'}`}>
              <strong>{firstText(targetLocation ?? option, ['targetLocationName', 'name', 'название'], 'Локация')}</strong>
              <small>{firstText(option, ['direction', 'description', 'описание'], firstText(targetLocation, ['description', 'описание'], exitId ? 'Переход' : 'Прямой переход'))}</small>
              <Button variant="secondary" disabled={!target || travel.isPending || disabled} onClick={() => target && travel.mutate({ targetLocationId: target, exitId })}>
                Перейти
              </Button>
            </div>
          );
        })}
      </div>
      {options.error ? <p className="form-error">{getErrorMessage(options.error)}</p> : null}
      {travel.error ? <p className="form-error">{getErrorMessage(travel.error)}</p> : null}
    </Panel>
  );
}

function CombatPanel({
  gameStateId,
  state,
  onChanged,
  activeCharacterId,
  permissions,
}: {
  gameStateId: string;
  state: PlayStateResponse;
  onChanged: () => void;
  activeCharacterId: string;
  permissions: PlayPermissions;
}) {
  const [attackerId, setAttackerId] = useState('');
  const [targetId, setTargetId] = useState('');
  const start = useMutation({ mutationFn: () => combatApi.start(gameStateId), onSuccess: onChanged });
  const end = useMutation({ mutationFn: () => combatApi.end(gameStateId), onSuccess: onChanged });
  const resolve = useMutation({ mutationFn: () => combatApi.resolveOutcome(gameStateId), onSuccess: onChanged });
  const continueCombat = useMutation({ mutationFn: () => combatApi.continue(gameStateId), onSuccess: onChanged });
  const participants = arrayOfObjects(state.combat?.participants ?? state.combat?.участники);
  const attackerOptions = permissions.canManage
    ? participants
    : participants.filter((participant) => textOf(participant.типАктера ?? participant.actorType, '') === 'character' && idOf(participant.actorId) === activeCharacterId);
  const attack = useMutation({
    mutationFn: () => combatApi.action(gameStateId, {
      attackerParticipantId: attackerId,
      targetParticipantId: targetId,
      attackRoll: '1d20',
      damageRoll: '1d6',
      damageType: 'physical',
      reason: 'Атака с игрового экрана.',
    }),
    onSuccess: onChanged,
  });

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
      {participants.length > 1 ? (
        <div className="form-stack">
          <select value={attackerId} onChange={(event) => setAttackerId(event.target.value)}>
            <option value="">Атакующий</option>
            {attackerOptions.map((participant) => <option key={String(participant.id)} value={String(participant.id)}>{firstText(participant, ['name', 'имя'], 'Участник')}</option>)}
          </select>
          <select value={targetId} onChange={(event) => setTargetId(event.target.value)}>
            <option value="">Цель</option>
            {participants.map((participant) => <option key={String(participant.id)} value={String(participant.id)}>{firstText(participant, ['name', 'имя'], 'Участник')}</option>)}
          </select>
          <Button disabled={!permissions.canPlay || !attackerId || !targetId || attackerId === targetId || attack.isPending} onClick={() => attack.mutate()}>
            Атаковать
          </Button>
        </div>
      ) : null}
      <div className="button-row">
        <Button variant="secondary" onClick={() => start.mutate()} disabled={start.isPending || !permissions.canPlay}>
          Начать
        </Button>
        <Button variant="secondary" onClick={() => resolve.mutate()} disabled={resolve.isPending || !permissions.canPlay}>
          Исход
        </Button>
        <Button variant="secondary" onClick={() => continueCombat.mutate()} disabled={continueCombat.isPending || !permissions.canPlay}>
          Продолжить бой
        </Button>
        <ConfirmButton variant="danger" confirmText="Завершить активный бой?" onConfirm={() => end.mutate()} disabled={end.isPending || !permissions.canPlay}>
          Завершить
        </ConfirmButton>
      </div>
      {[start.error, end.error, resolve.error, continueCombat.error, attack.error].filter(Boolean).map((error, index) => (
        <p className="form-error" key={index}>
          {getErrorMessage(error)}
        </p>
      ))}
    </Panel>
  );
}

function ProgressionPanel({ state }: { state: PlayStateResponse }) {
  const progression = objectOf(state.progression);

  return (
    <Panel title="Прогресс" actions={<Shield size={18} />}>
      <div className="metric-grid">
        <Metric label="Level" value={numberOf(progression?.level, 1)} />
        <Metric label="XP" value={numberOf(progression?.experience, 0)} />
        <Metric label="Next" value={numberOf(progression?.experienceToNextLevel, 300)} />
        <Metric label="Prof" value={numberOf(progression?.proficiencyBonus, 2)} />
      </div>
      <p className="muted">Опыт и повышение уровня выдаются игровыми наградами. Ручное управление доступно владельцу кампании.</p>
    </Panel>
  );
}

function RestTimePanel({
  gameStateId,
  characterId,
  state,
  onChanged,
  disabled,
}: {
  gameStateId: string;
  characterId: string;
  state: PlayStateResponse;
  onChanged: () => void;
  disabled: boolean;
}) {
  const time = objectOf(state.time);
  const selectedState = state.characterStates.find((item) => String(item.characterId ?? item.id) === characterId);
  const action = useMutation({
    mutationFn: async (kind: 'short-rest' | 'long-rest' | 'advance-time') => {
      if (kind === 'short-rest') return restApi.short(gameStateId, characterId);
      if (kind === 'long-rest') return restApi.long(gameStateId, characterId);
      return timeApi.advance(gameStateId, 60, 'Игровое время продвинуто через игровой экран.');
    },
    onSuccess: onChanged,
  });

  return (
    <Panel title="Время и состояние" actions={<Clock size={18} />}>
      <div className="metric-grid">
        <Metric label="День" value={numberOf(time?.day, 1)} />
        <Metric label="Час" value={numberOf(time?.hour, 0)} />
        <Metric label="Минуты" value={numberOf(time?.minute, 0)} />
        <Metric label="Всего" value={numberOf(time?.totalMinutes ?? time?.total_minutes, 0)} />
      </div>
      <div className="condition-list">
        {state.activeConditions.length === 0 ? <span className="muted">Активных состояний нет.</span> : null}
        {state.activeConditions.slice(0, 5).map((condition, index) => (
          <span className="badge" key={String(condition.id ?? index)}>
            {firstText(condition, ['name', 'название'], 'состояние')}
          </span>
        ))}
      </div>
      {selectedState ? (
        <div className="condition-list">
          {boolOf(selectedState.unconscious, false) ? <span className="badge badge-danger">без сознания</span> : null}
          {boolOf(selectedState.dead, false) ? <span className="badge badge-danger">мёртв</span> : null}
        </div>
      ) : null}
      <div className="button-row">
        <Button variant="secondary" disabled={action.isPending || disabled} onClick={() => action.mutate('short-rest')}>
          Короткий отдых
        </Button>
        <Button variant="secondary" disabled={action.isPending || disabled} onClick={() => action.mutate('long-rest')}>
          Долгий отдых
        </Button>
        <Button variant="secondary" disabled={action.isPending || disabled} onClick={() => action.mutate('advance-time')}>
          +1 час
        </Button>
      </div>
      {action.error ? <p className="form-error">{getErrorMessage(action.error)}</p> : null}
    </Panel>
  );
}

function LootPanel({
  gameStateId,
  characterId,
  state,
  onChanged,
  disabled,
}: {
  gameStateId: string;
  characterId: string;
  state: PlayStateResponse;
  onChanged: () => void;
  disabled: boolean;
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
          <Button variant="secondary" onClick={() => claim.mutate(String(loot.id))} disabled={claim.isPending || !characterId || disabled}>
            Забрать
          </Button>
        </div>
      ))}
      {claim.error ? <p className="form-error">{getErrorMessage(claim.error)}</p> : null}
    </Panel>
  );
}

function SnapshotsPanel({
  gameStateId,
  snapshots,
  canManage,
  onChanged,
}: {
  gameStateId: string;
  snapshots: SnapshotDto[];
  canManage: boolean;
  onChanged: () => void;
}) {
  const create = useMutation({
    mutationFn: () => snapshotsApi.create(gameStateId, 'Ручной snapshot из frontend.'),
    onSuccess: onChanged,
  });
  const restore = useMutation({
    mutationFn: (snapshotId: string) => snapshotsApi.restore(gameStateId, snapshotId),
    onSuccess: onChanged,
  });

  return (
    <Panel
      title="Snapshots"
      actions={
        canManage ? (
          <Button variant="secondary" disabled={create.isPending} onClick={() => create.mutate()}>
            <Camera size={18} /> Снимок
          </Button>
        ) : null
      }
    >
      {snapshots.length === 0 ? <EmptyState title="Снимков пока нет" /> : null}
      <div className="list-stack">
        {snapshots.slice(0, 5).map((snapshot) => (
          <div className="mini-card" key={snapshot.id}>
            <strong>{snapshot.reason || 'Snapshot'}</strong>
            <small>
              {new Date(snapshot.createdAt).toLocaleString()}
              {snapshot.restoredAt ? ` · restored ${new Date(snapshot.restoredAt).toLocaleString()}` : ''}
            </small>
            {canManage ? (
              <ConfirmButton
                variant="danger"
                disabled={restore.isPending}
                confirmText="Откатить игровое состояние к этому snapshot?"
                onConfirm={() => restore.mutate(snapshot.id)}
              >
                <Undo2 size={16} /> Откатиться
              </ConfirmButton>
            ) : null}
          </div>
        ))}
      </div>
      {[create.error, restore.error].filter(Boolean).map((error, index) => (
        <p className="form-error" key={index}>
          {getErrorMessage(error)}
        </p>
      ))}
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

const quickActionTemplates = [
  'Осмотреться',
  'Поговорить с ближайшим NPC',
  'Искать следы',
  'Проверить инвентарь',
  'Двигаться осторожно',
];

function describePayload(payload: JsonObject | null) {
  if (!payload) return 'Нет деталей';
  const ability = textOf(payload.ability ?? payload.характеристика, '');
  const dc = textOf(payload.difficultyClass ?? payload.сложность, '');
  const reason = textOf(payload.reason ?? payload.причина, '');
  return [ability && `характеристика: ${ability}`, dc && `сложность: ${dc}`, reason].filter(Boolean).join(' · ') || 'Проверка';
}

function payloadSummary(operation: string, payload: JsonObject | null) {
  if (!payload) return operationLabel(operation);

  const name = firstText(payload, ['name', 'title', 'название', 'имя'], '');
  const text = firstText(payload, ['text', 'entry', 'description', 'summary', 'текст', 'запись', 'описание'], '');
  const status = firstText(payload, ['status', 'статус'], '');
  const amount = textOf(payload.amount ?? payload.experience ?? payload.gold ?? payload.опыт ?? payload.золото, '');
  return truncate([name, text, status && `статус: ${status}`, amount && `значение: ${amount}`].filter(Boolean).join(' · ') || JSON.stringify(payload), 160);
}

function truncate(value: string, max: number) {
  return value.length <= max ? value : `${value.slice(0, max)}...`;
}
