import { useMemo, useState } from 'react';
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Copy, Link2, Send } from 'lucide-react';
import { invitesApi } from '../shared/api/endpoints';
import { queryKeys } from '../shared/api/query-keys';
import { AppShell, Button, EmptyState, ErrorState, LoadingState, Panel } from '../shared/components/ui';
import { getErrorMessage } from '../shared/api/errors';
import { useAuth } from '../auth/useAuth';

export function GameInvitePage() {
  const { gameStateId = '' } = useParams();
  const navigate = useNavigate();
  const [createdUrl, setCreatedUrl] = useState('');
  const create = useMutation({
    mutationFn: () => invitesApi.create(gameStateId, { role: 'player', expiresInHours: 168, maxUses: 1 }),
    onSuccess: (invite) => setCreatedUrl(`${window.location.origin}/invites/${invite.token}`),
  });

  async function copy() {
    if (createdUrl) {
      await navigator.clipboard.writeText(createdUrl);
    }
  }

  return (
    <AppShell
      title="Пригласить друга"
      subtitle="Ссылка даёт доступ к этой кампании как player."
      actions={<Button variant="secondary" onClick={() => navigate(`/games/${gameStateId}/play`)}>К игре</Button>}
    >
      <Panel title="Invite link">
        <p className="muted">Создай одноразовую ссылку и отправь её другу. Raw token показывается только здесь.</p>
        <div className="button-row">
          <Button disabled={create.isPending} onClick={() => create.mutate()}>
            <Link2 size={18} /> {create.isPending ? 'Создаём...' : 'Создать invite'}
          </Button>
          <Button variant="secondary" disabled={!createdUrl} onClick={copy}>
            <Copy size={18} /> Копировать
          </Button>
        </div>
        {createdUrl ? (
          <div className="notice">
            <strong>Ссылка:</strong> <Link to={new URL(createdUrl).pathname}>{createdUrl}</Link>
          </div>
        ) : null}
        {create.error ? <p className="form-error">{getErrorMessage(create.error)}</p> : null}
      </Panel>
    </AppShell>
  );
}

export function InviteAcceptPage() {
  const { token = '' } = useParams();
  const auth = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const preview = useQuery({
    queryKey: queryKeys.invite(token),
    queryFn: () => invitesApi.preview(token),
    enabled: Boolean(token),
    retry: false,
  });
  const accept = useMutation({
    mutationFn: () => invitesApi.accept(token),
    onSuccess: (result) => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.games });
      void queryClient.invalidateQueries({ queryKey: queryKeys.party(result.gameStateId) });
      navigate(`/games/${result.gameStateId}/play`, { replace: true });
    },
  });
  const loginState = useMemo(() => ({ from: location }), [location]);

  return (
    <AppShell title="Приглашение в игру" subtitle="Проверь кампанию и присоединись к партии.">
      <Panel title="Invite preview">
        {preview.isLoading ? <LoadingState text="Проверяем invite..." /> : null}
        {preview.error ? <ErrorState error={preview.error} /> : null}
        {preview.data ? (
          <div className="list-stack">
            <div className="mini-card">
              <strong>{preview.data.gameName}</strong>
              <small>
                Роль: {preview.data.role} · использований осталось: {preview.data.usesRemaining} · истекает{' '}
                {new Date(preview.data.expiresAt).toLocaleString()}
              </small>
            </div>
            {auth.isAuthenticated ? (
              <Button disabled={accept.isPending} onClick={() => accept.mutate()}>
                <Send size={18} /> {accept.isPending ? 'Присоединяемся...' : 'Присоединиться'}
              </Button>
            ) : (
              <div className="button-row">
                <Link className="btn btn-primary" to="/login" state={loginState}>
                  Войти и принять
                </Link>
                <Link className="btn btn-secondary" to="/register" state={loginState}>
                  Зарегистрироваться
                </Link>
              </div>
            )}
            {accept.error ? <p className="form-error">{getErrorMessage(accept.error)}</p> : null}
          </div>
        ) : null}
        {!preview.isLoading && !preview.data && !preview.error ? <EmptyState title="Invite не найден" /> : null}
      </Panel>
    </AppShell>
  );
}
