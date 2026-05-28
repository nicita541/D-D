using System.Text.Json;
using backend.Contracts.Rpg.Campaigns;
using backend.Infrastructure.Database;
using Npgsql;

namespace backend.Repositories.Rpg;

public sealed class CampaignRepository : ICampaignRepository
{
    private readonly IPostgresConnectionFactory _connectionFactory;

    public CampaignRepository(IPostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<JsonElement>> GetCampaignTemplatesAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT jsonb_build_object(
                'id', id,
                'название', title,
                'жанр', genre,
                'тон', tone,
                'краткоеописание', summary,
                'вступление', opening_scene,
                'главнаяцель', main_goal,
                'секретымастера', master_secrets,
                'начальныефлаги', initial_flags
            )::text
            FROM game.campaign_templates
            ORDER BY title;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new List<JsonElement>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(RpgDbJson.ParseElement(reader.GetString(0)));
        }

        return result;
    }

    public async Task<JsonElement?> GetCampaignTemplateAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT jsonb_build_object(
                'id', id,
                'название', title,
                'жанр', genre,
                'тон', tone,
                'краткоеописание', summary,
                'вступление', opening_scene,
                'главнаяцель', main_goal,
                'секретымастера', master_secrets,
                'начальныефлаги', initial_flags
            )::text
            FROM game.campaign_templates
            WHERE id = @id
            LIMIT 1;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : RpgDbJson.ParseElement(value.ToString()!);
    }

    public async Task<Guid> CreateCampaignTemplateAsync(CreateCampaignTemplateRequest request, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
            INSERT INTO game.campaign_templates
            (
                id,
                title,
                genre,
                tone,
                summary,
                opening_scene,
                main_goal,
                master_secrets,
                initial_flags
            )
            VALUES
            (
                gen_random_uuid(),
                @title,
                @genre,
                @tone,
                @summary,
                @openingScene,
                @mainGoal,
                @masterSecrets::jsonb,
                @initialFlags::jsonb
            )
            RETURNING id;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("title", request.Title.Trim());
        command.Parameters.AddWithValue("genre", RpgDbJson.DbString(request.Genre));
        command.Parameters.AddWithValue("tone", RpgDbJson.DbString(request.Tone));
        command.Parameters.AddWithValue("summary", RpgDbJson.DbString(request.Summary));
        command.Parameters.AddWithValue("openingScene", RpgDbJson.DbString(request.OpeningScene));
        command.Parameters.AddWithValue("mainGoal", RpgDbJson.DbString(request.MainGoal));
        command.Parameters.AddJsonb("masterSecrets", request.MasterSecrets);
        command.Parameters.AddJsonb("initialFlags", request.InitialFlags);

        return (Guid)(await command.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("Campaign template id was not returned."));
    }

    public async Task<bool> DeleteCampaignTemplateAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = "DELETE FROM game.campaign_templates WHERE id = @id;";
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }
}
