import { render, screen } from '@testing-library/react';
import { expect, it } from 'vitest';
import type { PlayStateResponse } from '../shared/api/types';
import { ChatLog } from './PlayPage';

it('hides system prompts and renders only visible player actions plus master answers', () => {
  const state = {
    recentTurns: [
      {
        id: 'system-turn',
        playerMessage: null,
        turnSource: 'system',
        masterAnswer: 'Вступление мастера',
      },
      {
        id: 'player-turn',
        playerMessage: 'Осмотреться',
        turnSource: 'player',
        masterAnswer: 'Ты замечаешь следы у дороги.',
      },
    ],
  } as unknown as PlayStateResponse;

  render(<ChatLog state={state} />);

  expect(screen.queryByText(/Начни новую одиночную RPG-сцену/i)).not.toBeInTheDocument();
  expect(screen.getByText('Вступление мастера')).toBeInTheDocument();
  expect(screen.getByText('Осмотреться')).toBeInTheDocument();
  expect(screen.getByText('Ты замечаешь следы у дороги.')).toBeInTheDocument();
});
