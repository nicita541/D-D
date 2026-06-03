using System.Text.Json;
using backend.Infrastructure.Database;
using Npgsql;

namespace backend.Modules.Ai.Infrastructure;

public sealed class AiMasterContextRepository : IAiMasterContextRepository
{
    private readonly IPostgresConnectionFactory _connectionFactory;

    public AiMasterContextRepository(IPostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<JsonElement?> GetContextAsync(
        Guid accountId,
        Guid gameStateId,
        int recentEventsLimit,
        CancellationToken cancellationToken)
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
                    '����������������', c.title,
                    '���', c.tone,
                    '������������', s.current_act,
                    '������������', s.current_scene,
                    '�����������', s.current_goal,
                    '����������', s.tension_level,
                    '�������������', s.plot_flags,
                    '�������������', s.known_facts,
                    '�������������', s.short_memory
                ) AS data
                FROM game.story_states s
                LEFT JOIN game.campaign_templates c ON c.id = s.campaign_template_id
                WHERE s.game_state_id = @gameStateId
                LIMIT 1
            ),
            party_doc AS (
                SELECT jsonb_build_object(
                    'id', p.id,
                    '��������', p.name,
                    '���������', COALESCE(
                        (
                            SELECT jsonb_agg(jsonb_build_object(
                                'id', m.id,
                                'accountId', m.account_id,
                                'characterId', m.character_id,
                                '����', m.role,
                                '������', m.status,
                                '���������������', m.display_name
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
                    '�������', c.is_active,
                    '�����', c.round_number,
                    '���������', COALESCE(
                        (
                            SELECT jsonb_agg(jsonb_build_object(
                                'id', p.id,
                                '���������', p.actor_type,
                                'actorId', p.actor_id,
                                '���', p.name,
                                '����������', p.initiative,
                                '���������', p.hp_current,
                                '����������', p.hp_max,
                                '�������������', p.has_acted,
                                '���������', p.conditions
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
                               '���������', turn_number,
                               '���', type,
                               '�����', text,
                               '������', important,
                               'createdAt', created_at
                           ) AS item
                    FROM game.game_log_entries
                    WHERE game_state_id = @gameStateId
                    ORDER BY turn_number DESC, created_at DESC
                    LIMIT @recentEventsLimit
                ) x
            ),
            recent_rolls AS (
                SELECT COALESCE(jsonb_agg(x.item ORDER BY x.created_at, x.id), '[]'::jsonb) AS data
                FROM (
                    SELECT id,
                           created_at,
                           jsonb_build_object(
                               'id', id,
                               'characterId', character_id,
                               'formula', formula,
                               'reason', reason,
                               'rolls', rolls,
                               'modifier', modifier,
                               'total', total,
                               'createdAt', created_at
                           ) AS item
                    FROM game.dice_rolls
                    WHERE game_state_id = @gameStateId
                    ORDER BY created_at DESC, id DESC
                    LIMIT 10
                ) x
            ),
            recent_checks AS (
                SELECT COALESCE(jsonb_agg(x.item ORDER BY x.created_at, x.id), '[]'::jsonb) AS data
                FROM (
                    SELECT sc.id,
                           sc.created_at,
                           jsonb_build_object(
                               'id', sc.id,
                               'characterId', sc.character_id,
                               'ability', sc.ability,
                               'difficultyClass', sc.difficulty_class,
                               'total', sc.total,
                               'success', sc.success,
                               'reason', sc.reason,
                               'createdAt', sc.created_at
                           ) AS item
                    FROM game.skill_checks sc
                    WHERE sc.game_state_id = @gameStateId
                    ORDER BY sc.created_at DESC, sc.id DESC
                    LIMIT 10
                ) x
            ),
            mechanic_requests_doc AS (
                SELECT jsonb_build_object(
                    'pending', COALESCE((
                        SELECT jsonb_agg(jsonb_build_object(
                            'id', mr.id,
                            'requestType', mr.request_type,
                            'payload', mr.payload,
                            'status', mr.status,
                            'createdAt', mr.created_at
                        ) ORDER BY mr.created_at DESC, mr.id DESC)
                        FROM game.mechanic_requests mr
                        WHERE mr.game_state_id = @gameStateId
                          AND mr.status = 'pending'
                    ), '[]'::jsonb),
                    'resolved', COALESCE((
                        SELECT jsonb_agg(x.item ORDER BY x.resolved_at DESC NULLS LAST, x.id DESC)
                        FROM (
                            SELECT mr.id,
                                   mr.resolved_at,
                                   jsonb_build_object(
                                       'id', mr.id,
                                       'requestType', mr.request_type,
                                       'payload', mr.payload,
                                       'result', mr.result,
                                       'status', mr.status,
                                       'resolvedAt', mr.resolved_at
                                   ) AS item
                            FROM game.mechanic_requests mr
                            WHERE mr.game_state_id = @gameStateId
                              AND mr.status = 'resolved'
                            ORDER BY mr.resolved_at DESC NULLS LAST, mr.id DESC
                            LIMIT 10
                        ) x
                    ), '[]'::jsonb)
                ) AS data
            ),
            memory_doc AS (
                SELECT jsonb_build_object(
                    '������', cm.summary,
                    '������������', cm.current_scene,
                    '�����������', cm.important_facts,
                    '�������������', cm.open_threads,
                    '�������������', cm.resolved_threads,
                    '���������Npc', cm.known_npcs,
                    '����������������', cm.known_locations,
                    '��������������', cm.master_secrets,
                    'updatedAt', cm.updated_at
                ) AS data
                FROM game.campaign_memories cm
                WHERE cm.game_state_id = @gameStateId
                LIMIT 1
            )
            SELECT jsonb_build_object(
                'gameStateId', sd.game_state_id,
                'номерХода', (sd.data->>'номерХода')::int,
                'название', sd.data->>'название',
                'сюжет', COALESCE((SELECT data FROM story_doc), '{}'::jsonb),
                'партия', COALESCE((SELECT data FROM party_doc), '{}'::jsonb),
                'персонажи', COALESCE(sd.data->'персонажи', '[]'::jsonb),
                'мир', sd.data->'мир',
                'боевоеСостояниеДокумента', sd.data->'бой',
                'последниеЗаписиЖурнала', COALESCE((SELECT data FROM recent_history), '[]'::jsonb),
                'последниеБроски', COALESCE((SELECT data FROM recent_rolls), '[]'::jsonb),
                'последниеПроверки', COALESCE((SELECT data FROM recent_checks), '[]'::jsonb),
                'запросыМеханик', COALESCE(
                    (SELECT data FROM mechanic_requests_doc),
                    jsonb_build_object('pending', '[]'::jsonb, 'resolved', '[]'::jsonb)
                ),
                'памятьКампании', COALESCE(
                    (SELECT data FROM memory_doc),
                    jsonb_build_object(
                        'резюме', '',
                        'текущаяСцена', '{}'::jsonb,
                        'важныеФакты', '[]'::jsonb,
                        'открытыеЛинии', '[]'::jsonb,
                        'закрытыеЛинии', '[]'::jsonb,
                        'известныеNpc', '[]'::jsonb,
                        'известныеЛокации', '[]'::jsonb,
                        'секретыМастера', '[]'::jsonb
                    )
                ),
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
