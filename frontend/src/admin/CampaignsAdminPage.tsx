import { useNavigate } from 'react-router-dom';
import { ArrowLeft } from 'lucide-react';
import { campaignsApi } from '../shared/api/endpoints';
import { queryKeys } from '../shared/api/query-keys';
import { AppShell, Button } from '../shared/components/ui';
import { ResourceManager } from '../shared/components/management';

export function CampaignsAdminPage() {
  const navigate = useNavigate();
  return (
    <AppShell
      title="Шаблоны кампаний"
      subtitle="Административный раздел"
      actions={<Button variant="ghost" onClick={() => navigate('/games')}><ArrowLeft size={18} /> Кампании</Button>}
    >
      <ResourceManager
        title="Публичные шаблоны"
        queryKey={queryKeys.campaigns}
        list={() => campaignsApi.list()}
        get={(id) => campaignsApi.get(id)}
        create={(body) => campaignsApi.create(body)}
        remove={(id) => campaignsApi.delete(id)}
        defaultValue={{ название: 'Новый шаблон', жанр: '', тон: '', краткоеописание: '', вступление: '', главнаяцель: '', секретымастера: [], начальныефлаги: {} }}
        nameKey="название"
        descriptionKey="краткоеописание"
      />
    </AppShell>
  );
}
