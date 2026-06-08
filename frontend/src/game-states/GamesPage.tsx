import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link2, LogOut, Plus, Settings, Shield, Swords, Trash2 } from 'lucide-react';
import { campaignsApi, gameStatesApi, storyApi } from '../shared/api/endpoints';
import { Button, AppShell, ConfirmButton, EmptyState, ErrorState, Field, LoadingState, Panel } from '../shared/components/ui';
import { useAuth } from '../auth/useAuth';
import { getErrorMessage } from '../shared/api/errors';
import { queryKeys } from '../shared/api/query-keys';

export function GamesPage() {
  const auth = useAuth();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [name, setName] = useState('');
  const [templateId, setTemplateId] = useState('');
  const games = useQuery({ queryKey: queryKeys.games, queryFn: gameStatesApi.list });
  const templates = useQuery({ queryKey: queryKeys.campaigns, queryFn: campaignsApi.list });
  const create = useMutation({
    mutationFn: async (gameName: string) => {
      const game = await gameStatesApi.create(gameName);
      const template = templates.data?.find((item) => item.id === templateId);
      if (template) {
        await storyApi.update(game.id, {
          campaignTemplateId: template.id,
          текущаяглава: 'Начало',
          текущаясцена: template.вступление ?? '',
          текущаяцель: template.главнаяцель ?? '',
          напряжение: 0,
          сюжетныефлаги: template.начальныефлаги,
          открытыеФакты: [],
          скрытыеФакты: template.секретымастера,
          краткаяПамять: template.краткоеописание ? [template.краткоеописание] : [],
        });
      }
      return game;
    },
    onSuccess: (game) => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.games });
      navigate(`/games/${game.id}/manage/characters`);
    },
  });
  const remove = useMutation({
    mutationFn: (id: string) => gameStatesApi.delete(id),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: queryKeys.games }),
  });

  function submit(event: FormEvent) {
    event.preventDefault();
    if (!name.trim()) return;
    create.mutate(name.trim());
  }

  return (
    <AppShell
      title="Кампании"
      subtitle={`Аккаунт: ${auth.session?.account.displayName ?? auth.session?.account.username}`}
      actions={
        <>
          {auth.session?.account.role === 'admin' ? (
            <Button variant="secondary" onClick={() => navigate('/admin/campaigns')}>
              <Shield size={18} /> Шаблоны
            </Button>
          ) : null}
          <Button
            variant="secondary"
            onClick={async () => {
              await auth.logout();
              navigate('/login', { replace: true });
            }}
          >
            <LogOut size={18} /> Выйти
          </Button>
        </>
      }
    >
      <div className="two-column">
        <Panel title="Создать игру">
          <form className="form-stack" onSubmit={submit}>
            <Field label="Название кампании">
              <input value={name} onChange={(event) => setName(event.target.value)} placeholder="Тени Серебряного тракта" />
            </Field>
            <Field label="Публичный шаблон (необязательно)">
              <select value={templateId} onChange={(event) => setTemplateId(event.target.value)}>
                <option value="">Без шаблона</option>
                {templates.data?.map((template) => <option key={template.id} value={template.id}>{template.название}</option>)}
              </select>
            </Field>
            <Button type="submit" disabled={create.isPending}>
              <Plus size={18} /> {create.isPending ? 'Создаём...' : 'Создать'}
            </Button>
            {create.error ? <p className="form-error">{getErrorMessage(create.error)}</p> : null}
          </form>
        </Panel>

        <Panel title="Мои игры">
          {games.isLoading ? <LoadingState /> : null}
          {games.error ? <ErrorState error={games.error} /> : null}
          {games.data?.length === 0 ? (
            <EmptyState title="Игр пока нет" text="Создай кампанию и добавь первого персонажа." />
          ) : null}
          <div className="list-stack">
            {games.data?.map((game) => (
              <div className="game-row" key={game.id}>
                <Swords size={18} />
                <span>{String(game.name ?? game.название ?? 'Без названия')}</span>
                <small>{game.id}</small>
                <div className="game-row-actions">
                  <Button variant="secondary" onClick={() => navigate(`/games/${game.id}/manage/characters`)}>
                    <Settings size={16} /> Управление
                  </Button>
                  <Button onClick={() => navigate(`/games/${game.id}/play`)}>Продолжить</Button>
                  <Button variant="secondary" onClick={() => navigate(`/games/${game.id}/invite`)}>
                    <Link2 size={16} /> Invite
                  </Button>
                  <ConfirmButton
                    variant="danger"
                    disabled={remove.isPending}
                    confirmText={`Удалить кампанию «${String(game.name ?? game.название ?? '')}» со всеми данными?`}
                    onConfirm={() => remove.mutate(game.id)}
                  >
                    <Trash2 size={16} /> Удалить
                  </ConfirmButton>
                </div>
              </div>
            ))}
          </div>
          {remove.error ? <p className="form-error">{getErrorMessage(remove.error)}</p> : null}
        </Panel>
      </div>
    </AppShell>
  );
}
