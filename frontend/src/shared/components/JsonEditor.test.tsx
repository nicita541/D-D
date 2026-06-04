import { fireEvent, render, screen } from '@testing-library/react';
import { expect, it, vi } from 'vitest';
import { JsonEditor } from './JsonEditor';

it('validates JSON objects before updating the document', () => {
  const onChange = vi.fn();
  render(<JsonEditor value={{ tags: [] }} onChange={onChange} />);

  fireEvent.change(screen.getByLabelText('Расширенные JSON-поля'), { target: { value: '{bad' } });
  expect(screen.getByText(/Expected property name/i)).toBeInTheDocument();
  expect(onChange).not.toHaveBeenCalled();

  fireEvent.change(screen.getByLabelText('Расширенные JSON-поля'), { target: { value: '{"tags":["quest"]}' } });
  expect(onChange).toHaveBeenLastCalledWith({ tags: ['quest'] });
});
