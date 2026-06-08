import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Dice5, Save } from 'lucide-react';
import { accountCharactersApi } from '../shared/api/endpoints';
import { queryKeys } from '../shared/api/query-keys';
import type { AccountCharacter } from '../shared/api/types';
import { AppShell, Button, ErrorState, Field, LoadingState, Panel } from '../shared/components/ui';
import { getErrorMessage } from '../shared/api/errors';
import { characterName } from './display';

export function CharacterEditorPage() {
  const { characterId } = useParams();
  const isEdit = Boolean(characterId);
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const character = useQuery({
    queryKey: queryKeys.accountCharacter(characterId ?? ''),
    queryFn: () => accountCharactersApi.get(characterId ?? ''),
    enabled: isEdit,
  });
  const [form, setForm] = useState({
    name: '',
    species: '',
    className: '',
    background: '',
    description: '',
  });
  const [advanced, setAdvanced] = useState<Record<string, string>>({});
  const [advancedError, setAdvancedError] = useState('');

  useEffect(() => {
    if (!character.data) return;
    // Query data replaces the edit buffer when the route loads a different hero.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    setForm({
      name: character.data.name ?? '',
      species: character.data.species ?? '',
      className: character.data.className ?? '',
      background: character.data.background ?? '',
      description: character.data.description ?? '',
    });
    setAdvanced(toAdvancedText(character.data));
  }, [character.data]);

  const generate = useMutation({
    mutationFn: () => accountCharactersApi.generate(cleanForm(form)),
    onSuccess: async (created) => {
      await queryClient.invalidateQueries({ queryKey: queryKeys.accountCharacters });
      navigate(`/launch?characterId=${created.id}`);
    },
  });
  const update = useMutation({
    mutationFn: (body: Record<string, unknown>) => accountCharactersApi.update(characterId ?? '', body),
    onSuccess: async (updated) => {
      await queryClient.invalidateQueries({ queryKey: queryKeys.accountCharacters });
      await queryClient.invalidateQueries({ queryKey: queryKeys.accountCharacter(updated.id) });
      navigate('/characters');
    },
  });

  const title = isEdit ? `Редактировать: ${characterName(character.data)}` : 'Новый герой';
  const subtitle = isEdit
    ? 'Быстрые поля сверху, полный JSON-редактор в Advanced.'
    : 'Заполни только то, что хочешь. Остальное backend сгенерирует сам.';

  function submit(event: FormEvent) {
    event.preventDefault();
    setAdvancedError('');
    if (!isEdit) {
      generate.mutate();
      return;
    }

    const parsed = parseAdvanced(advanced);
    if (!parsed.ok) {
      setAdvancedError(parsed.error);
      return;
    }

    update.mutate({
      ...cleanForm(form),
      ...parsed.value,
    });
  }

  const pending = generate.isPending || update.isPending;
  const error = generate.error ?? update.error;

  return (
    <AppShell
      title={title}
      subtitle={subtitle}
      actions={
        <>
          <Link className="btn btn-ghost" to="/characters">
            Персонажи
          </Link>
          <Link className="btn btn-secondary" to="/home">
            Меню
          </Link>
        </>
      }
    >
      {character.isLoading ? <LoadingState text="Загружаем героя..." /> : null}
      {character.error ? <ErrorState error={character.error} /> : null}
      {!character.isLoading && !character.error ? (
        <Panel>
          <form className="form-stack" onSubmit={submit}>
            <div className="form-grid two">
              <Field label="Имя">
                <input value={form.name} onChange={(event) => setForm((value) => ({ ...value, name: event.target.value }))} placeholder="Можно оставить пустым" />
              </Field>
              <Field label="Раса / вид">
                <input value={form.species} onChange={(event) => setForm((value) => ({ ...value, species: event.target.value }))} placeholder="например, человек" />
              </Field>
              <Field label="Класс">
                <input value={form.className} onChange={(event) => setForm((value) => ({ ...value, className: event.target.value }))} placeholder="например, воин" />
              </Field>
              <Field label="Предыстория">
                <input value={form.background} onChange={(event) => setForm((value) => ({ ...value, background: event.target.value }))} placeholder="например, странник" />
              </Field>
            </div>
            <Field label="Краткое описание">
              <textarea value={form.description} onChange={(event) => setForm((value) => ({ ...value, description: event.target.value }))} />
            </Field>

            {isEdit ? <AdvancedEditor value={advanced} onChange={setAdvanced} /> : null}
            {advancedError ? <p className="form-error">{advancedError}</p> : null}
            {error ? <p className="form-error">{getErrorMessage(error)}</p> : null}

            <Button disabled={pending} type="submit">
              {isEdit ? <Save size={18} /> : <Dice5 size={18} />}
              {isEdit ? 'Сохранить' : 'Сгенерировать основу и играть'}
            </Button>
          </form>
        </Panel>
      ) : null}
    </AppShell>
  );
}

function AdvancedEditor({
  value,
  onChange,
}: {
  value: Record<string, string>;
  onChange: (value: Record<string, string>) => void;
}) {
  const sections = useMemo(
    () => [
      ['attributes', 'Характеристики'],
      ['resources', 'HP / ресурсы'],
      ['progression', 'Прогрессия'],
      ['wealth', 'Деньги'],
      ['combat', 'Бой'],
      ['inventory', 'Инвентарь'],
      ['equipment', 'Экипировка'],
      ['attacks', 'Атаки'],
      ['metadata', 'Metadata'],
    ] as const,
    [],
  );

  return (
    <details className="json-editor">
      <summary>Advanced: полное состояние героя</summary>
      <div className="form-grid two">
        {sections.map(([key, label]) => (
          <Field label={label} key={key}>
            <textarea
              rows={7}
              spellCheck={false}
              value={value[key] ?? ''}
              onChange={(event) => onChange({ ...value, [key]: event.target.value })}
            />
          </Field>
        ))}
      </div>
    </details>
  );
}

function toAdvancedText(character: AccountCharacter) {
  return {
    attributes: JSON.stringify(character.attributes ?? {}, null, 2),
    resources: JSON.stringify(character.resources ?? {}, null, 2),
    progression: JSON.stringify(character.progression ?? {}, null, 2),
    wealth: JSON.stringify(character.wealth ?? {}, null, 2),
    combat: JSON.stringify(character.combat ?? {}, null, 2),
    inventory: JSON.stringify(character.inventory ?? [], null, 2),
    equipment: JSON.stringify(character.equipment ?? {}, null, 2),
    attacks: JSON.stringify(character.attacks ?? [], null, 2),
    metadata: JSON.stringify(character.metadata ?? {}, null, 2),
  };
}

function cleanForm(form: Record<string, string>) {
  return Object.fromEntries(Object.entries(form).map(([key, value]) => [key, value.trim()]).filter(([, value]) => value));
}

function parseAdvanced(value: Record<string, string>): { ok: true; value: Record<string, unknown> } | { ok: false; error: string } {
  const parsed: Record<string, unknown> = {};
  for (const [key, text] of Object.entries(value)) {
    try {
      parsed[key] = JSON.parse(text);
    } catch {
      return { ok: false, error: `Некорректный JSON в блоке ${key}.` };
    }
  }
  return { ok: true, value: parsed };
}
