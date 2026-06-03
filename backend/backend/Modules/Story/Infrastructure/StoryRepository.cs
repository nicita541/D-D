using System.Text.Json;
using backend.Modules.Story.Contracts;
using backend.Infrastructure.Database;
using Npgsql;

namespace backend.Modules.Story.Infrastructure;

public sealed class StoryRepository : IStoryRepository
{
    private readonly IPostgresConnectionFactory _connectionFactory;

    public StoryRepository(IPostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<JsonElement?> GetStoryStateAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT jsonb_build_object(
                'id', s.id,
                'gameStateId', s.game_state_id,
                'campaignTemplateId', s.campaign_template_id,
                'названиеКампании', c.title,
                'текущаяглава', s.current_act,
                'текущаясцена', s.current_scene,
                'текущаяцель', s.current_goal,
                'напряжение', s.tension_level,
                'сюжетныефлаги', s.plot_flags,
                'открытыеФакты', s.known_facts,
                'скрытыеФакты', s.hidden_facts,
                'краткаяПамять', s.short_memory
            )::text
            FROM game.story_states s
            JOIN game.game_states gs ON gs.id = s.game_state_id
            LEFT JOIN game.campaign_templates c ON c.id = s.campaign_template_id
            WHERE s.game_state_id = @gameStateId
              AND gs.account_id = @accountId
            LIMIT 1;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : RpgDbJson.ParseElement(value.ToString()!);
    }

    public async Task<Guid?> UpsertStoryStateAsync(Guid accountId, Guid gameStateId, CreateOrUpdateStoryStateRequest request, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
            INSERT INTO game.story_states
            (
                id,
                game_state_id,
                campaign_template_id,
                current_act,
                current_scene,
                current_goal,
                tension_level,
                plot_flags,
                known_facts,
                hidden_facts,
                short_memory
            )
            SELECT
                gen_random_uuid(),
                @gameStateId,
                @campaignTemplateId,
                @currentAct,
                @currentScene,
                @currentGoal,
                @tensionLevel,
                @plotFlags::jsonb,
                @knownFacts::jsonb,
                @hiddenFacts::jsonb,
                @shortMemory::jsonb
            WHERE EXISTS (
                SELECT 1
                FROM game.game_states
                WHERE id = @gameStateId
                  AND account_id = @accountId
            )
            ON CONFLICT (game_state_id)
            DO UPDATE SET
                campaign_template_id = EXCLUDED.campaign_template_id,
                current_act = EXCLUDED.current_act,
                current_scene = EXCLUDED.current_scene,
                current_goal = EXCLUDED.current_goal,
                tension_level = EXCLUDED.tension_level,
                plot_flags = EXCLUDED.plot_flags,
                known_facts = EXCLUDED.known_facts,
                hidden_facts = EXCLUDED.hidden_facts,
                short_memory = EXCLUDED.short_memory,
                updated_at = now()
            RETURNING id;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddNullableUuid("campaignTemplateId", request.CampaignTemplateId);
        command.Parameters.AddWithValue("currentAct", RpgDbJson.DbString(request.CurrentAct));
        command.Parameters.AddWithValue("currentScene", RpgDbJson.DbString(request.CurrentScene));
        command.Parameters.AddWithValue("currentGoal", RpgDbJson.DbString(request.CurrentGoal));
        command.Parameters.AddWithValue("tensionLevel", request.TensionLevel < 0 ? 0 : request.TensionLevel);
        command.Parameters.AddJsonb("plotFlags", request.PlotFlags);
        command.Parameters.AddJsonb("knownFacts", request.KnownFacts);
        command.Parameters.AddJsonb("hiddenFacts", request.HiddenFacts);
        command.Parameters.AddJsonb("shortMemory", request.ShortMemory);

        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : (Guid)value;
    }
}
