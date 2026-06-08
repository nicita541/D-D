import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { BookOpen } from 'lucide-react';
import { campaignsApi } from '../shared/api/endpoints';
import { queryKeys } from '../shared/api/query-keys';
import { AppShell, ErrorState, LoadingState } from '../shared/components/ui';
import { campaignGenre, campaignOpening, campaignSummary, campaignTitle, campaignTone } from './display';

export function StoriesPage() {
  const campaigns = useQuery({ queryKey: queryKeys.campaigns, queryFn: campaignsApi.list });

  return (
    <AppShell
      title="Истории"
      subtitle="Это готовые завязки для игры, не админский CRUD."
      actions={
        <>
          <Link className="btn btn-ghost" to="/home">
            Меню
          </Link>
          <Link className="btn btn-primary" to="/launch">
            Выбрать героя и историю
          </Link>
        </>
      }
    >
      {campaigns.isLoading ? <LoadingState text="Загружаем истории..." /> : null}
      {campaigns.error ? <ErrorState error={campaigns.error} /> : null}
      <div className="card-grid">
        {campaigns.data?.slice(0, 6).map((campaign) => (
          <article className="story-card" key={campaign.id}>
            <p className="eyebrow">
              <BookOpen size={14} /> {campaignGenre(campaign)} · {campaignTone(campaign)}
            </p>
            <h2>{campaignTitle(campaign)}</h2>
            <p>{campaignSummary(campaign)}</p>
            {campaignOpening(campaign) ? <blockquote>{campaignOpening(campaign)}</blockquote> : null}
            <Link className="btn btn-primary" to={`/launch?campaignTemplateId=${campaign.id}`}>
              Выбрать историю
            </Link>
          </article>
        ))}
      </div>
    </AppShell>
  );
}
