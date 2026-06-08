import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { BookOpen, Play, UserRound, Users } from 'lucide-react';
import { accountCharactersApi, gameStatesApi } from '../shared/api/endpoints';
import { queryKeys } from '../shared/api/query-keys';
import { textOf } from '../shared/api/json';
import { AppShell, EmptyState, ErrorState, LoadingState, Panel } from '../shared/components/ui';
import { characterClass, characterLevel, characterName, characterSpecies } from './display';

export function HomePage() {
  const characters = useQuery({ queryKey: queryKeys.accountCharacters, queryFn: accountCharactersApi.list });
  const games = useQuery({ queryKey: queryKeys.games, queryFn: gameStatesApi.list });
  const hasCharacters = (characters.data?.length ?? 0) > 0;
  const hasGames = (games.data?.length ?? 0) > 0;

  return (
    <AppShell
      title="Игровое меню"
      subtitle="Выбери постоянного героя, историю и запускай Solo без админ-панели."
      actions={
        <Link className="btn btn-ghost" to="/games">
          Старые сохранения
        </Link>
      }
    >
      <div className="home-hero panel">
        <div>
          <p className="eyebrow">быстрый старт</p>
          <h2>Герой живёт на аккаунте и переносит прогресс между историями.</h2>
          <p className="muted">
            Создай основу одним кликом, выбери одну из готовых историй, режим Solo и нажми «Начать».
          </p>
        </div>
        <div className="home-actions">
          <Link className="btn btn-primary" to={hasCharacters ? '/launch' : '/characters/new'}>
            <Play size={18} /> Играть
          </Link>
          <Link className="btn btn-secondary" to="/characters">
            <UserRound size={18} /> Персонажи
          </Link>
          <Link className="btn btn-secondary" to="/stories">
            <BookOpen size={18} /> Истории
          </Link>
        </div>
      </div>

      <div className="player-grid">
        <Panel title="Мои герои">
          {characters.isLoading ? <LoadingState text="Загружаем героев..." /> : null}
          {characters.error ? <ErrorState error={characters.error} /> : null}
          {!characters.isLoading && !characters.error && !hasCharacters ? (
            <EmptyState title="Героев пока нет" text="Создай основу, backend сам выставит статы, HP, AC и стартовый профиль." />
          ) : null}
          <div className="card-grid compact">
            {characters.data?.slice(0, 4).map((character) => (
              <Link className="player-card" key={character.id} to={`/launch?characterId=${character.id}`}>
                <strong>{characterName(character)}</strong>
                <span>
                  {characterSpecies(character)} · {characterClass(character)} · уровень {characterLevel(character)}
                </span>
              </Link>
            ))}
          </div>
          <div className="button-row">
            <Link className="btn btn-secondary" to="/characters">
              Все персонажи
            </Link>
            <Link className="btn btn-primary" to="/characters/new">
              Создать героя
            </Link>
          </div>
        </Panel>

        <Panel title="Продолжить игру" actions={<Users size={18} />}>
          {games.isLoading ? <LoadingState text="Загружаем сохранения..." /> : null}
          {games.error ? <ErrorState error={games.error} /> : null}
          {!games.isLoading && !games.error && !hasGames ? (
            <EmptyState title="Нет активных историй" text="Запусти новую Solo-сессию через выбор истории." />
          ) : null}
          <div className="list-stack">
            {games.data?.slice(0, 5).map((game) => (
              <Link className="game-row one-line" key={game.id} to={`/games/${game.id}/play`}>
                <strong>{textOf(game.name, 'Игра')}</strong>
                <small>Ход {game.turnNumber ?? 0}</small>
              </Link>
            ))}
          </div>
        </Panel>
      </div>
    </AppShell>
  );
}
