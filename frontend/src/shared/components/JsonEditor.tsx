import { useEffect, useState } from 'react';
import type { JsonObject, JsonValue } from '../api/types';

export function JsonEditor({
  value,
  onChange,
  label = 'Расширенные JSON-поля',
  minRows = 10,
}: {
  value: JsonObject;
  onChange: (value: JsonObject) => void;
  label?: string;
  minRows?: number;
}) {
  const [text, setText] = useState(() => JSON.stringify(value, null, 2));
  const [error, setError] = useState('');

  useEffect(() => {
    // External document loads replace the editor buffer; invalid local drafts stay local until then.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    setText(JSON.stringify(value, null, 2));
    setError('');
  }, [value]);

  function update(next: string) {
    setText(next);
    try {
      const parsed = JSON.parse(next) as JsonValue;
      if (!parsed || Array.isArray(parsed) || typeof parsed !== 'object') {
        throw new Error('Нужен JSON object.');
      }
      setError('');
      onChange(parsed as JsonObject);
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Некорректный JSON.');
    }
  }

  return (
    <details className="json-editor">
      <summary>{label}</summary>
      <textarea
        aria-label={label}
        className={error ? 'invalid' : ''}
        rows={minRows}
        spellCheck={false}
        value={text}
        onChange={(event) => update(event.target.value)}
      />
      {error ? <p className="form-error">{error}</p> : null}
    </details>
  );
}
