import { useParams } from 'react-router-dom';
import { storyApi } from '../shared/api/endpoints';
import { queryKeys } from '../shared/api/query-keys';
import { textOf } from '../shared/api/json';
import { GameManageShell } from '../shared/components/GameManageShell';
import { ActionConsole, DocumentEditor, ReadOnlyDocument } from '../shared/components/management';

export function StoryManagePage() {
  const { gameStateId = '' } = useParams();

  return (
    <GameManageShell title="Сюжет, память и партия">
      <div className="manage-grid">
        <DocumentEditor
          title="Сюжетное состояние"
          queryKey={queryKeys.story(gameStateId)}
          load={() => storyApi.get(gameStateId)}
          save={(body) => storyApi.update(gameStateId, body)}
          defaultValue={{ campaignTemplateId: null, текущаяглава: '', текущаясцена: '', текущаяцель: '', напряжение: 0, сюжетныефлаги: {}, открытыеФакты: [], скрытыеФакты: [], краткаяПамять: [] }}
          secret
        />
        <DocumentEditor
          title="Долгая память кампании"
          queryKey={queryKeys.memory(gameStateId)}
          load={() => storyApi.getMemory(gameStateId)}
          save={(body) => storyApi.updateMemory(gameStateId, body)}
          defaultValue={{ резюме: '', текущаяСцена: {}, важныеФакты: [], открытыеЛинии: [], закрытыеЛинии: [], известныеNpc: [], известныеЛокации: [], секретыМастера: [] }}
          secret
        />
        <ReadOnlyDocument title="Партия" queryKey={queryKeys.party(gameStateId)} load={() => storyApi.getParty(gameStateId)} />
      </div>
      <div className="command-grid">
        <ActionConsole
          title="Суммаризация памяти AI"
          description="Обновляет долгую память по последним ходам. GM-секреты остаются только в management."
          actionLabel="Суммаризировать"
          initialBody={{ recentTurnsLimit: 20 }}
          action={(body) => storyApi.summarizeMemory(gameStateId, body)}
        />
        <ActionConsole title="Создать партию" actionLabel="Создать" initialBody={{ название: 'Партия героев' }} action={(body) => storyApi.createParty(gameStateId, body)} />
        <ActionConsole
          title="Добавить участника партии"
          actionLabel="Добавить"
          initialBody={{ characterId: '', role: 'member', status: 'active', displayName: '' }}
          action={(body) => storyApi.addPartyMember(gameStateId, body)}
        />
        <ActionConsole
          title="Удалить участника партии"
          actionLabel="Удалить"
          initialBody={{ memberId: '' }}
          action={(body) => storyApi.removePartyMember(gameStateId, textOf(body.memberId))}
          danger
        />
      </div>
    </GameManageShell>
  );
}
