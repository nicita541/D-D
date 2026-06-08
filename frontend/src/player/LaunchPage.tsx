import { useMemo, useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { useMutation, useQuery } from '@tanstack/react-query';
import { Play, Users } from 'lucide-react';
import { accountCharactersApi, campaignsApi, gameSessionsApi } from '../shared/api/endpoints';
import { queryKeys } from '../shared/api/query-keys';
import { AppShell, Button, EmptyState, ErrorState, LoadingState, Panel } from '../shared/components/ui';
import { getErrorMessage } from '../shared/api/errors';
import { campaignSummary, campaignTitle, characterClass, characterLevel, characterName, characterSpecies } from './display';

export function LaunchPage() {
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const characters = useQuery({ queryKey: queryKeys.accountCharacters, queryFn: accountCharactersApi.list });
  const campaigns = useQuery({ queryKey: queryKeys.campaigns, queryFn: campaignsApi.list });
  const [selectedCharacterId, setSelectedCharacterId] = useState(params.get('characterId') ?? '');
  const [selectedCampaignTemplateId, setSelectedCampaignTemplateId] = useState(params.get('campaignTemplateId') ?? '');
  const characterId = selectedCharacterId || characters.data?.[0]?.id || '';
  const campaignTemplateId = selectedCampaignTemplateId || campaigns.data?.[0]?.id || '';

  const selectedCharacter = useMemo(
    () => characters.data?.find((character) => character.id === characterId),
    [characterId, characters.data],
  );
  const selectedCampaign = useMemo(
    () => campaigns.data?.find((campaign) => campaign.id === campaignTemplateId),
    [campaignTemplateId, campaigns.data],
  );
  const start = useMutation({
    mutationFn: () => gameSessionsApi.start({ accountCharacterId: characterId, campaignTemplateId, mode: 'solo' }),
    onSuccess: (session) => navigate(session.playUrl),
  });

  return (
    <AppShell
      title="Запуск игры"
      subtitle="Выбери героя, историю и режим. Сейчас основной режим — Solo."
      actions={
        <>
          <Link className="btn btn-ghost" to="/home">
            Меню
          </Link>
          <Link className="btn btn-secondary" to="/characters">
            Персонажи
          </Link>
          <Link className="btn btn-secondary" to="/stories">
            Истории
          </Link>
        </>
      }
    >
      {characters.isLoading || campaigns.isLoading ? <LoadingState text="Готовим запуск..." /> : null}
      {characters.error ? <ErrorState error={characters.error} /> : null}
      {campaigns.error ? <ErrorState error={campaigns.error} /> : null}

      {!characters.isLoading && characters.data?.length === 0 ? (
        <Panel>
          <EmptyState title="Сначала нужен герой" text="Создай основу одним кликом, потом вернёшься к выбору истории." />
          <Link className="btn btn-primary" to="/characters/new">
            Создать героя
          </Link>
        </Panel>
      ) : (
        <div className="launch-grid">
          <Panel title="1. Герой">
            <select value={characterId} onChange={(event) => setSelectedCharacterId(event.target.value)}>
              {characters.data?.map((character) => (
                <option key={character.id} value={character.id}>
                  {characterName(character)} · {characterClass(character)} · уровень {characterLevel(character)}
                </option>
              ))}
            </select>
            {selectedCharacter ? (
              <div className="player-card selected">
                <strong>{characterName(selectedCharacter)}</strong>
                <span>
                  {characterSpecies(selectedCharacter)} · {characterClass(selectedCharacter)} · уровень {characterLevel(selectedCharacter)}
                </span>
                <small>{selectedCharacter.description || selectedCharacter.background || 'Готов к первой сцене.'}</small>
              </div>
            ) : null}
          </Panel>

          <Panel title="2. История">
            <select value={campaignTemplateId} onChange={(event) => setSelectedCampaignTemplateId(event.target.value)}>
              {campaigns.data?.map((campaign) => (
                <option key={campaign.id} value={campaign.id}>
                  {campaignTitle(campaign)}
                </option>
              ))}
            </select>
            {selectedCampaign ? (
              <div className="story-card selected">
                <h2>{campaignTitle(selectedCampaign)}</h2>
                <p>{campaignSummary(selectedCampaign)}</p>
              </div>
            ) : null}
          </Panel>

          <Panel title="3. Режим">
            <div className="mode-grid">
              <button className="mode-card active" type="button">
                <Play size={22} />
                <strong>Solo</strong>
                <span>Игрок и AI-мастер. Доступно сейчас.</span>
              </button>
              <button className="mode-card" type="button" disabled>
                <Users size={22} />
                <strong>Party</strong>
                <span>Co-op через invite. Скоро в главном flow.</span>
              </button>
            </div>
            <Button disabled={!characterId || !campaignTemplateId || start.isPending} onClick={() => start.mutate()}>
              <Play size={18} /> {start.isPending ? 'Запускаем...' : 'Сыграть'}
            </Button>
            {start.error ? <p className="form-error">{getErrorMessage(start.error)}</p> : null}
          </Panel>
        </div>
      )}
    </AppShell>
  );
}
