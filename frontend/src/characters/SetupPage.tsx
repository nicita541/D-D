import { useState, type FormEvent } from 'react';
import { useMutation, useQuery } from '@tanstack/react-query';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { ArrowLeft, Sparkles } from 'lucide-react';
import { charactersApi, playApi } from '../shared/api/endpoints';
import { AppShell, Button, ErrorState, Field, LoadingState, Panel } from '../shared/components/ui';
import { getErrorMessage } from '../shared/api/errors';
import { defaultCharacter, toCreateCharacterPayload, type CharacterFormData } from './character-payload';

export function SetupPage() {
  const { gameStateId = '' } = useParams();
  const navigate = useNavigate();
  const [form, setForm] = useState<CharacterFormData>(defaultCharacter);
  const characters = useQuery({
    queryKey: ['characters', gameStateId],
    queryFn: () => charactersApi.list(gameStateId),
    enabled: Boolean(gameStateId),
  });
  const create = useMutation({
    mutationFn: async () => {
      await charactersApi.create(gameStateId, toCreateCharacterPayload(form));
      try {
        await playApi.bootstrap(gameStateId);
      } catch {
        // Bootstrap may return conflict if the game was already prepared; setup can still continue.
      }
    },
    onSuccess: () => navigate(`/games/${gameStateId}/play`),
  });

  function submit(event: FormEvent) {
    event.preventDefault();
    create.mutate();
  }

  return (
    <AppShell
      title="Подготовка персонажа"
      subtitle="Создай героя, затем frontend подготовит стартовую сцену через play/bootstrap."
      actions={
        <Button variant="secondary" onClick={() => navigate('/games')}>
          <ArrowLeft size={18} /> К играм
        </Button>
      }
    >
      <div className="two-column wide-left">
        <Panel title="Новый персонаж">
          <form className="form-grid" onSubmit={submit}>
            <Field label="Имя">
              <input value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} required />
            </Field>
            <Field label="Вид">
              <input value={form.species} onChange={(event) => setForm({ ...form, species: event.target.value })} />
            </Field>
            <Field label="Класс">
              <input value={form.className} onChange={(event) => setForm({ ...form, className: event.target.value })} />
            </Field>
            <Field label="Золото">
              <input type="number" value={form.gold} onChange={(event) => setForm({ ...form, gold: Number(event.target.value) })} />
            </Field>
            <Field label="Предыстория">
              <textarea value={form.background} onChange={(event) => setForm({ ...form, background: event.target.value })} />
            </Field>
            <Field label="Описание">
              <textarea value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} />
            </Field>

            <div className="stat-grid">
              {(['strength', 'dexterity', 'constitution', 'intelligence', 'wisdom', 'charisma'] as const).map((key) => (
                <Field key={key} label={statLabels[key]}>
                  <input type="number" value={form[key]} onChange={(event) => setForm({ ...form, [key]: Number(event.target.value) })} />
                </Field>
              ))}
              <Field label="HP">
                <input type="number" value={form.hpMax} onChange={(event) => setForm({ ...form, hpMax: Number(event.target.value) })} />
              </Field>
              <Field label="КД">
                <input type="number" value={form.armorClass} onChange={(event) => setForm({ ...form, armorClass: Number(event.target.value) })} />
              </Field>
            </div>

            {create.error ? <p className="form-error">{getErrorMessage(create.error)}</p> : null}
            <Button type="submit" disabled={create.isPending}>
              <Sparkles size={18} /> {create.isPending ? 'Создаём...' : 'Создать героя и начать'}
            </Button>
          </form>
        </Panel>

        <Panel title="Уже есть персонажи">
          {characters.isLoading ? <LoadingState /> : null}
          {characters.error ? <ErrorState error={characters.error} /> : null}
          <div className="list-stack">
            {characters.data?.map((character) => (
              <Link className="game-row" key={String(character.id)} to={`/games/${gameStateId}/play`}>
                <span>{String(character.name ?? character.имя ?? 'Персонаж')}</span>
                <small>{String(character.className ?? character.класс ?? '')}</small>
              </Link>
            ))}
          </div>
          {characters.data && characters.data.length > 0 ? (
            <Button onClick={() => navigate(`/games/${gameStateId}/play`)}>Перейти к игре</Button>
          ) : null}
        </Panel>
      </div>
    </AppShell>
  );
}

const statLabels = {
  strength: 'Сила',
  dexterity: 'Ловкость',
  constitution: 'Телосложение',
  intelligence: 'Интеллект',
  wisdom: 'Мудрость',
  charisma: 'Харизма',
};
