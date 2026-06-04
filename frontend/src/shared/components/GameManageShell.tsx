import type { ReactNode } from 'react';
import { NavLink, useNavigate, useParams } from 'react-router-dom';
import { ArrowLeft, Gamepad2 } from 'lucide-react';
import { AppShell, Button } from './ui';
import { useQuery } from '@tanstack/react-query';
import { gameStatesApi } from '../api/endpoints';
import { queryKeys } from '../api/query-keys';

const sections = [
  ['characters', 'Персонажи'],
  ['world', 'Мир'],
  ['story', 'Сюжет и память'],
  ['encounters', 'Бои и награды'],
  ['history', 'История и AI'],
] as const;

export function GameManageShell({ title, children, actions }: { title: string; children: ReactNode; actions?: ReactNode }) {
  const { gameStateId = '' } = useParams();
  const navigate = useNavigate();
  const game = useQuery({ queryKey: queryKeys.game(gameStateId), queryFn: () => gameStatesApi.get(gameStateId), enabled: Boolean(gameStateId) });

  return (
    <AppShell
      title={title}
      subtitle={`${String(game.data?.name ?? game.data?.название ?? 'Кампания')} · раздел владельца; секреты мастера доступны только здесь.`}
      actions={
        <>
          {actions}
          <Button variant="secondary" onClick={() => navigate(`/games/${gameStateId}/play`)}>
            <Gamepad2 size={18} /> Играть
          </Button>
          <Button variant="ghost" onClick={() => navigate('/games')}>
            <ArrowLeft size={18} /> Кампании
          </Button>
        </>
      }
    >
      <nav className="manage-nav" aria-label="Управление кампанией">
        {sections.map(([path, label]) => (
          <NavLink key={path} to={`/games/${gameStateId}/manage/${path}`}>
            {label}
          </NavLink>
        ))}
      </nav>
      {children}
    </AppShell>
  );
}
