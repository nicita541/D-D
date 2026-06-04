import { useEffect, useState } from 'react';
import { useMutation, useQuery, useQueryClient, type QueryKey } from '@tanstack/react-query';
import { Braces, Plus, Save, Trash2 } from 'lucide-react';
import type { JsonObject } from '../api/types';
import { firstText, idOf } from '../api/json';
import { getErrorMessage } from '../api/errors';
import { Button, ConfirmButton, EmptyState, ErrorState, Field, LoadingState, Notice, Panel } from './ui';
import { JsonEditor } from './JsonEditor';

interface ResourceManagerProps {
  title: string;
  queryKey: QueryKey;
  list: () => Promise<JsonObject[]>;
  get?: (id: string) => Promise<JsonObject>;
  create?: (body: JsonObject) => Promise<unknown>;
  update?: (id: string, body: JsonObject) => Promise<unknown>;
  remove?: (id: string) => Promise<unknown>;
  defaultValue?: JsonObject;
  nameKey?: string;
  descriptionKey?: string;
}

export function ResourceManager({
  title,
  queryKey,
  list,
  get,
  create,
  update,
  remove,
  defaultValue = {},
  nameKey = 'name',
  descriptionKey = 'description',
}: ResourceManagerProps) {
  const queryClient = useQueryClient();
  const query = useQuery({ queryKey, queryFn: list });
  const [selectedId, setSelectedId] = useState('');
  const [draft, setDraft] = useState<JsonObject>(() => ({ ...defaultValue }));
  const [message, setMessage] = useState('');
  const [selectionError, setSelectionError] = useState('');

  async function selectItem(id: string, item: JsonObject) {
    setSelectedId(id);
    setDraft({ ...item });
    setMessage('');
    setSelectionError('');
    if (get) {
      try {
        setDraft(await get(id));
      } catch (error) {
        setSelectionError(getErrorMessage(error));
      }
    }
  }

  const mutation = useMutation({
    mutationFn: async (mode: 'create' | 'update' | 'delete') => {
      if (mode === 'create' && create) return create(draft);
      if (mode === 'update' && update && selectedId) return update(selectedId, draft);
      if (mode === 'delete' && remove && selectedId) return remove(selectedId);
      throw new Error('Операция не поддерживается.');
    },
    onSuccess: (_, mode) => {
      setMessage(mode === 'delete' ? 'Запись удалена.' : 'Изменения сохранены.');
      if (mode === 'delete') setSelectedId('');
      void queryClient.invalidateQueries({ queryKey });
    },
  });

  const titleValue = String(draft[nameKey] ?? '');
  const descriptionValue = String(draft[descriptionKey] ?? '');

  return (
    <Panel title={title} actions={<Braces size={18} />}>
      {query.isLoading ? <LoadingState /> : null}
      {query.error ? <ErrorState error={query.error} /> : null}
      <div className="resource-layout">
        <div className="resource-list">
          <Button variant={!selectedId ? 'primary' : 'secondary'} onClick={() => { setSelectedId(''); setDraft({ ...defaultValue }); setMessage(''); }}>
            <Plus size={16} /> Новая запись
          </Button>
          {query.data?.length === 0 ? <EmptyState title="Записей нет" /> : null}
          {query.data?.map((item, index) => {
            const id = idOf(item.id) ?? String(index);
            return (
              <button className={`resource-row ${selectedId === id ? 'active' : ''}`} key={id} onClick={() => void selectItem(id, item)}>
                <strong>{firstText(item, [nameKey, 'name', 'title', 'название', 'имя'], 'Без названия')}</strong>
                <small>{id}</small>
              </button>
            );
          })}
        </div>
        <div className="form-stack">
          <Field label={nameKey === 'title' ? 'Название' : 'Имя / название'}>
            <input value={titleValue} onChange={(event) => setDraft({ ...draft, [nameKey]: event.target.value })} />
          </Field>
          <Field label="Описание">
            <textarea value={descriptionValue} onChange={(event) => setDraft({ ...draft, [descriptionKey]: event.target.value })} />
          </Field>
          <JsonEditor value={draft} onChange={setDraft} />
          <div className="button-row">
            {!selectedId && create ? (
              <Button disabled={mutation.isPending} onClick={() => mutation.mutate('create')}>
                <Plus size={16} /> Создать
              </Button>
            ) : null}
            {selectedId && update ? (
              <Button disabled={mutation.isPending} onClick={() => mutation.mutate('update')}>
                <Save size={16} /> Сохранить
              </Button>
            ) : null}
            {selectedId && remove ? (
              <ConfirmButton
                variant="danger"
                disabled={mutation.isPending}
                confirmText={`Удалить запись «${titleValue || selectedId}»?`}
                onConfirm={() => mutation.mutate('delete')}
              >
                <Trash2 size={16} /> Удалить
              </ConfirmButton>
            ) : null}
          </div>
          {message ? <Notice>{message}</Notice> : null}
          {selectionError ? <p className="form-error">{selectionError}</p> : null}
          {mutation.error ? <p className="form-error">{getErrorMessage(mutation.error)}</p> : null}
        </div>
      </div>
    </Panel>
  );
}

export function DocumentEditor({
  title,
  queryKey,
  load,
  save,
  defaultValue = {},
  secret = false,
}: {
  title: string;
  queryKey: QueryKey;
  load: () => Promise<JsonObject>;
  save: (body: JsonObject) => Promise<unknown>;
  defaultValue?: JsonObject;
  secret?: boolean;
}) {
  const queryClient = useQueryClient();
  const query = useQuery({ queryKey, queryFn: load, retry: false });
  const [draft, setDraft] = useState<JsonObject>(defaultValue);
  const mutation = useMutation({
    mutationFn: () => save(draft),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey }),
  });

  useEffect(() => {
    if (query.data) {
      // The server document becomes the initial editable draft when it arrives.
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setDraft(query.data);
    }
  }, [query.data]);

  return (
    <Panel title={title}>
      {secret ? <div className="warning-box">GM-only: содержимое не показывается в игровом режиме.</div> : null}
      {query.isLoading ? <LoadingState /> : null}
      {query.error ? <p className="muted">Документ ещё не создан или недоступен. Можно заполнить и сохранить новый.</p> : null}
      <JsonEditor value={draft} onChange={setDraft} label="Редактор документа JSON" minRows={16} />
      <Button disabled={mutation.isPending} onClick={() => mutation.mutate()}>
        <Save size={16} /> Сохранить
      </Button>
      {mutation.isSuccess ? <Notice>Документ сохранён.</Notice> : null}
      {mutation.error ? <p className="form-error">{getErrorMessage(mutation.error)}</p> : null}
    </Panel>
  );
}

export function ActionConsole({
  title,
  description,
  actionLabel,
  initialBody = {},
  action,
  danger = false,
}: {
  title: string;
  description?: string;
  actionLabel: string;
  initialBody?: JsonObject;
  action: (body: JsonObject) => Promise<unknown>;
  danger?: boolean;
}) {
  const [body, setBody] = useState<JsonObject>(initialBody);
  const mutation = useMutation({ mutationFn: () => action(body) });
  const button = danger ? (
    <ConfirmButton variant="danger" confirmText={`${actionLabel}?`} onConfirm={() => mutation.mutate()} disabled={mutation.isPending}>
      {actionLabel}
    </ConfirmButton>
  ) : (
    <Button onClick={() => mutation.mutate()} disabled={mutation.isPending}>{actionLabel}</Button>
  );

  return (
    <Panel title={title}>
      {description ? <p className="muted">{description}</p> : null}
      <JsonEditor value={body} onChange={setBody} label="Параметры операции" minRows={7} />
      {button}
      {mutation.isSuccess ? <Notice>Операция выполнена.</Notice> : null}
      {mutation.error ? <p className="form-error">{getErrorMessage(mutation.error)}</p> : null}
      {mutation.data ? <pre>{JSON.stringify(mutation.data, null, 2)}</pre> : null}
    </Panel>
  );
}

export function ReadOnlyDocument({ title, queryKey, load }: { title: string; queryKey: QueryKey; load: () => Promise<unknown> }) {
  const query = useQuery({ queryKey, queryFn: load });
  return (
    <Panel title={title}>
      {query.isLoading ? <LoadingState /> : null}
      {query.error ? <ErrorState error={query.error} /> : null}
      {query.data ? <pre className="document-viewer">{JSON.stringify(query.data, null, 2)}</pre> : null}
    </Panel>
  );
}
