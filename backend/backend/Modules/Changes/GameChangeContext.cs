using System.Text.Json;
using Npgsql;

namespace backend.Modules.Changes;

public sealed class GameChangeContext
{
    private readonly Func<string, JsonElement, CancellationToken, Task<JsonElement>> _applyCanonicalOperationAsync;

    public GameChangeContext(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid accountId,
        Guid gameStateId,
        Guid changeId,
        Guid? gameTurnId,
        Func<string, JsonElement, CancellationToken, Task<JsonElement>> applyCanonicalOperationAsync)
    {
        Connection = connection;
        Transaction = transaction;
        AccountId = accountId;
        GameStateId = gameStateId;
        ChangeId = changeId;
        GameTurnId = gameTurnId;
        _applyCanonicalOperationAsync = applyCanonicalOperationAsync;
    }

    public NpgsqlConnection Connection { get; }

    public NpgsqlTransaction Transaction { get; }

    public Guid AccountId { get; }

    public Guid GameStateId { get; }

    public Guid ChangeId { get; }

    public Guid? GameTurnId { get; }

    public Task<JsonElement> ApplyCanonicalOperationAsync(
        string canonicalOperation,
        JsonElement payload,
        CancellationToken cancellationToken)
        => _applyCanonicalOperationAsync(canonicalOperation, payload, cancellationToken);
}
