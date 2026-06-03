import { useState, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { LogOut, Plus, Swords } from 'lucide-react';
import { gameStatesApi } from '../shared/api/endpoints';
import { Button, AppShell, EmptyState, ErrorState, Field, LoadingState, Panel } from '../shared/components/ui';
import { useAuth } from '../auth/useAuth';

export function GamesPage() {
  const auth = useAuth();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [name, setName] = useState('');
  const games = useQuery({ queryKey: ['game-states'], queryFn: gameStatesApi.list });
  const create = useMutation({
    mutationFn: (gameName: string) => gameStatesApi.create(gameName),
    onSuccess: (game) => {
      void queryClient.invalidateQueries({ queryKey: ['game-states'] });
      navigate(`/games/${game.id}/setup`);
    },
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
        <Button
          variant="secondary"
          onClick={async () => {
            await auth.logout();
            navigate('/login', { replace: true });
          }}
        >
          <LogOut size={18} /> Выйти
        </Button>
      }
    >
      <div className="two-column">
        <Panel title="Создать игру">
          <form className="form-stack" onSubmit={submit}>
            <Field label="Название кампании">
              <input value={name} onChange={(event) => setName(event.target.value)} placeholder="Тени Серебряного тракта" />
            </Field>
            <Button type="submit" disabled={create.isPending}>
              <Plus size={18} /> {create.isPending ? 'Создаём...' : 'Создать'}
            </Button>
            {create.error ? <p className="form-error">{create.error.message}</p> : null}
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
              <Link className="game-row" key={game.id} to={`/games/${game.id}/play`}>
                <Swords size={18} />
                <span>{String(game.name ?? game.название ?? 'Без названия')}</span>
                <small>{game.id}</small>
              </Link>
            ))}
          </div>
        </Panel>
      </div>
    </AppShell>
  );
}
