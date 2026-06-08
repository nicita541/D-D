import { Link } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Plus, Trash2 } from 'lucide-react';
import { accountCharactersApi } from '../shared/api/endpoints';
import { queryKeys } from '../shared/api/query-keys';
import { AppShell, ConfirmButton, EmptyState, ErrorState, LoadingState, Panel } from '../shared/components/ui';
import { getErrorMessage } from '../shared/api/errors';
import { characterAc, characterClass, characterGold, characterHp, characterLevel, characterName, characterSpecies } from './display';

export function CharactersPage() {
  const queryClient = useQueryClient();
  const characters = useQuery({ queryKey: queryKeys.accountCharacters, queryFn: accountCharactersApi.list });
  const remove = useMutation({
    mutationFn: accountCharactersApi.delete,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.accountCharacters }),
  });

  return (
    <AppShell
      title="Персонажи"
      subtitle="Это постоянные герои аккаунта. Их уровень, вещи и ресурсы переносятся между историями."
      actions={
        <>
          <Link className="btn btn-ghost" to="/home">
            Меню
          </Link>
          <Link className="btn btn-primary" to="/characters/new">
            <Plus size={18} /> Новый герой
          </Link>
        </>
      }
    >
      {characters.isLoading ? <LoadingState text="Загружаем героев..." /> : null}
      {characters.error ? <ErrorState error={characters.error} /> : null}
      {!characters.isLoading && !characters.error && characters.data?.length === 0 ? (
        <Panel>
          <EmptyState title="Героев пока нет" text="Создай основу, без ручной настройки каждого стата." />
          <Link className="btn btn-primary" to="/characters/new">
            Сгенерировать героя
          </Link>
        </Panel>
      ) : null}

      <div className="card-grid">
        {characters.data?.map((character) => {
          const hp = characterHp(character);
          return (
            <article className="character-card" key={character.id}>
              <div>
                <p className="eyebrow">уровень {characterLevel(character)}</p>
                <h2>{characterName(character)}</h2>
                <p className="muted">
                  {characterSpecies(character)} · {characterClass(character)}
                </p>
                <p>{character.description || character.background || 'Герой без длинной биографии. История начнётся за столом.'}</p>
              </div>
              <div className="metric-grid">
                <Metric label="HP" value={`${hp.current}/${hp.max}`} />
                <Metric label="AC" value={characterAc(character)} />
                <Metric label="Золото" value={characterGold(character)} />
                <Metric label="Предметы" value={character.inventory?.length ?? 0} />
              </div>
              <div className="button-row">
                <Link className="btn btn-primary" to={`/launch?characterId=${character.id}`}>
                  Играть
                </Link>
                <Link className="btn btn-secondary" to={`/characters/${character.id}/edit`}>
                  Редактировать
                </Link>
                <ConfirmButton
                  variant="danger"
                  disabled={remove.isPending}
                  confirmText={`Удалить героя ${characterName(character)}?`}
                  onConfirm={() => remove.mutate(character.id)}
                >
                  <Trash2 size={16} /> Удалить
                </ConfirmButton>
              </div>
            </article>
          );
        })}
      </div>
      {remove.error ? <p className="form-error">{getErrorMessage(remove.error)}</p> : null}
    </AppShell>
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
