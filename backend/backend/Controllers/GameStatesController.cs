using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace backend.Controllers
{
    /// <summary>
    /// Temporary GameState controller for the new PostgreSQL GameState schema.
    ///
    /// This controller works directly with the database while the real Auth + Game services
    /// are not finished yet. It uses a local development account internally.
    ///
    /// Main database objects used:
    /// - auth.accounts
    /// - game.game_states
    /// - game.create_new_game(...)
    /// - game.game_state_documents
    /// </summary>
    [ApiController]
    [Route("api/game-states")]
    public class GameStatesController : ControllerBase
    {
        private static readonly JsonDocumentOptions JsonDocumentOptions = new()
        {
            AllowTrailingCommas = true
        };

        private readonly string _connectionString;

        public GameStatesController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DndDatabase")
                ?? throw new InvalidOperationException("Connection string 'DndDatabase' not found.");
        }

        /// <summary>
        /// Returns all active GameState documents.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<List<JsonElement>>> GetGameStates()
        {
            var documents = new List<JsonElement>();

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = """
                SELECT data::text
                FROM game.game_state_documents
                ORDER BY data->>'id';
            """;

            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var json = reader.GetString(0);
                documents.Add(ParseJsonElement(json));
            }

            return Ok(documents);
        }

        /// <summary>
        /// Returns one full GameState document by game_state UUID.
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<JsonElement>> GetGameState(Guid id)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = """
                SELECT data::text
                FROM game.game_state_documents
                WHERE game_state_id = @id
                LIMIT 1;
            """;

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("id", id);

            var result = await command.ExecuteScalarAsync();

            if (result == null || result == DBNull.Value)
            {
                return NotFound(new
                {
                    message = "Сохранение GameState не найдено",
                    id
                });
            }

            return Ok(ParseJsonElement(result.ToString()!));
        }

        /// <summary>
        /// Returns only the player object from a GameState document.
        /// Useful while the old frontend still expects a player-like payload.
        /// </summary>
        [HttpGet("{id:guid}/player")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<JsonElement>> GetGameStatePlayer(Guid id)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = """
                SELECT (data->'игрок')::text
                FROM game.game_state_documents
                WHERE game_state_id = @id
                LIMIT 1;
            """;

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("id", id);

            var result = await command.ExecuteScalarAsync();

            if (result == null || result == DBNull.Value)
            {
                return NotFound(new
                {
                    message = "Игрок в сохранении GameState не найден",
                    id
                });
            }

            return Ok(ParseJsonElement(result.ToString()!));
        }

        /// <summary>
        /// Creates a new GameState using game.create_new_game(...).
        ///
        /// Until real auth is implemented, this endpoint uses a local development account:
        /// dev-local@example.com / dev-local.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<object>> CreateGameState([FromBody] CreateGameStateRequest? request)
        {
            var saveName = string.IsNullOrWhiteSpace(request?.Name)
                ? "Новая игра"
                : request.Name.Trim();

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                var accountId = await EnsureDevelopmentAccountAsync(connection, transaction);
                var gameStateId = await CreateNewGameAsync(connection, transaction, accountId, saveName);

                await transaction.CommitAsync();

                return CreatedAtAction(
                    nameof(GetGameState),
                    new { id = gameStateId },
                    new
                    {
                        id = gameStateId,
                        message = "GameState создан",
                        accountId,
                        saveName
                    }
                );
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Soft deletes a GameState by setting game.game_states.is_active = false.
        /// </summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<object>> DeleteGameState(Guid id)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = """
                UPDATE game.game_states
                SET is_active = false,
                    updated_at = now()
                WHERE id = @id
                  AND is_active = true
                RETURNING id;
            """;

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("id", id);

            var result = await command.ExecuteScalarAsync();

            if (result == null || result == DBNull.Value)
            {
                return NotFound(new
                {
                    message = "Активное сохранение GameState не найдено",
                    id
                });
            }

            return Ok(new
            {
                id,
                message = "GameState удалён мягко: is_active = false"
            });
        }

        private static async Task<Guid> EnsureDevelopmentAccountAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction)
        {
            var accountId = Guid.NewGuid();
            var fakeHash = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes($"dev-local:{accountId}"))
            ).ToLowerInvariant();

            const string sql = """
                WITH inserted AS (
                    INSERT INTO auth.accounts
                    (
                        id,
                        email,
                        username,
                        password_hash,
                        display_name
                    )
                    VALUES
                    (
                        @id,
                        'dev-local@example.com',
                        'dev-local',
                        @passwordHash,
                        'Development Local Account'
                    )
                    ON CONFLICT (email) DO NOTHING
                    RETURNING id
                )
                SELECT id FROM inserted
                UNION ALL
                SELECT id
                FROM auth.accounts
                WHERE email = 'dev-local@example.com'
                LIMIT 1;
            """;

            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("id", accountId);
            command.Parameters.AddWithValue("passwordHash", fakeHash);

            return (Guid)(await command.ExecuteScalarAsync()
                ?? throw new InvalidOperationException("Could not create or load development account."));
        }

        private static async Task<Guid> CreateNewGameAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Guid accountId,
            string saveName)
        {
            const string sql = """
                SELECT game.create_new_game(@accountId, @saveName);
            """;

            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("accountId", accountId);
            command.Parameters.AddWithValue("saveName", saveName);

            return (Guid)(await command.ExecuteScalarAsync()
                ?? throw new InvalidOperationException("game.create_new_game did not return a GameState id."));
        }

        private static JsonElement ParseJsonElement(string json)
        {
            using var document = JsonDocument.Parse(json, JsonDocumentOptions);
            return document.RootElement.Clone();
        }
    }

    public class CreateGameStateRequest
    {
        [JsonPropertyName("название")]
        public string? Name { get; set; }
    }
}
