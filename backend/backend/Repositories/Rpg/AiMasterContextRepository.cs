using System.Text.Json;
using backend.Infrastructure.Database;
using Npgsql;
namespace backend.Repositories.Rpg;

public sealed class AiMasterContextRepository : IAiMasterContextRepository
{
    private readonly IPostgresConnectionFactory _connectionFactory;

    public AiMasterContextRepository(IPostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<JsonElement?> GetContextAsync(Guid accountId, Guid gameStateId, int recentEventsLimit, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
            WITH state_doc AS (
                SELECT game_state_id, data
                FROM game.game_state_documents
                WHERE game_state_id = @gameStateId
                  AND account_id = @accountId
                LIMIT 1
            ),
            story_doc AS (
                SELECT jsonb_build_object(
                    'id', s.id,
                    'campaignTemplateId', s.campaign_template_id,
                    'названиеКампании', c.title,
                    'тон', c.tone,
                    'текущаяглава', s.current_act,
                    'текущаясцена', s.current_scene,
                    'текущаяцель', s.current_goal,
                    'напряжение', s.tension_level,
                    'сюжетныефлаги', s.plot_flags,
                    'открытыеФакты', s.known_facts,
                    'краткаяПамять', s.short_memory
                ) AS data
                FROM game.story_states s
                LEFT JOIN game.campaign_templates c ON c.id = s.campaign_template_id
                WHERE s.game_state_id = @gameStateId
                LIMIT 1
            ),
            party_doc AS (
                SELECT jsonb_build_object(
                    'id', p.id,
                    'название', p.name,
                    'участники', COALESCE(
                        (
                            SELECT jsonb_agg(jsonb_build_object(
                                'id', m.id,
                                'accountId', m.account_id,
                                'characterId', m.character_id,
                                'роль', m.role,
                                'статус', m.status,
                                'отображаемоеИмя', m.display_name
                            ) ORDER BY m.created_at)
                            FROM game.party_members m
                            WHERE m.party_id = p.id
                        ),
                        '[]'::jsonb
                    )
                ) AS data
                FROM game.parties p
                WHERE p.game_state_id = @gameStateId
                LIMIT 1
            ),
            combat_doc AS (
                SELECT jsonb_build_object(
                    'id', c.id,
                    'активен', c.is_active,
                    'раунд', c.round_number,
                    'участники', COALESCE(
                        (
                            SELECT jsonb_agg(jsonb_build_object(
                                'id', p.id,
                                'типАктера', p.actor_type,
                                'actorId', p.actor_id,
                                'имя', p.name,
                                'инициатива', p.initiative,
                                'хпТекущее', p.hp_current,
                                'хпМаксимум', p.hp_max,
                                'ужеДействовал', p.has_acted,
                                'состояния', p.conditions
                            ) ORDER BY p.initiative DESC, p.created_at)
                            FROM game.combat_participants p
                            WHERE p.combat_state_id = c.id
                        ),
                        '[]'::jsonb
                    )
                ) AS data
                FROM game.combat_states c
                WHERE c.game_state_id = @gameStateId
                LIMIT 1
            ),
            recent_history AS (
                SELECT COALESCE(jsonb_agg(x.item ORDER BY x.turn_number), '[]'::jsonb) AS data
                FROM (
                    SELECT turn_number,
                           jsonb_build_object(
                               'номерхода', turn_number,
                               'тип', type,
                               'текст', text,
                               'важное', important,
                               'createdAt', created_at
                           ) AS item
                    FROM game.game_log_entries
                    WHERE game_state_id = @gameStateId
                    ORDER BY turn_number DESC, created_at DESC
                    LIMIT @recentEventsLimit
                ) x
            )
            SELECT jsonb_build_object(
                'gameStateId', sd.game_state_id,
                'номерхода', (sd.data->>'номерхода')::int,
                'режим', sd.data->>'режим',
                'сюжет', COALESCE((SELECT data FROM story_doc), '{}'::jsonb),
                'партия', COALESCE((SELECT data FROM party_doc), '{}'::jsonb),
                'игрок', sd.data->'игрок',
                'мир', sd.data->'мир',
                'квесты', sd.data->'квесты',
                'последниеСобытия', COALESCE((SELECT data FROM recent_history), '[]'::jsonb),
                'бой', COALESCE((SELECT data FROM combat_doc), '{}'::jsonb)
            )::text
            FROM state_doc sd;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("recentEventsLimit", recentEventsLimit <= 0 ? 10 : recentEventsLimit);

        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : RpgDbJson.ParseElement(value.ToString()!);
    }
}
