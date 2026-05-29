using System.Text.Json;
using backend.Contracts.Rpg.Common;
using backend.Contracts.Rpg.Turns;
using backend.Infrastructure.Ai;
using backend.Repositories.Rpg;

namespace backend.Services.Rpg;

public sealed class TurnService : ITurnService
{
    private readonly ITurnRepository _turns;
    private readonly IAiMasterContextService _context;
    private readonly IPromptBuilder _promptBuilder;
    private readonly IOllamaClient _ollama;

    public TurnService(
        ITurnRepository turns,
        IAiMasterContextService context,
        IPromptBuilder promptBuilder,
        IOllamaClient ollama)
    {
        _turns = turns;
        _context = context;
        _promptBuilder = promptBuilder;
        _ollama = ollama;
    }

    public async Task<RpgResult<JsonElement>> CreateTurnAsync(Guid accountId, Guid gameStateId, CreateTurnRequest request, CancellationToken cancellationToken)
    {
        var playerMessage = request.ResolvedPlayerMessage;
        if (string.IsNullOrWhiteSpace(playerMessage))
        {
            return RpgResult<JsonElement>.BadRequest("Player message is required.");
        }

        var turn = await _turns.CreatePendingTurnAsync(accountId, gameStateId, playerMessage, cancellationToken);
        if (turn is null)
        {
            return RpgResult<JsonElement>.NotFound("GameState was not found.");
        }

        try
        {
            var context = await _context.GetContextAsync(accountId, gameStateId, recentEventsLimit: 20, cancellationToken);
            if (!context.HasValue)
            {
                var failed = await _turns.FailTurnAsync(turn, "AI context was not found.", null, null, cancellationToken);
                return failed.HasValue
                    ? RpgResult<JsonElement>.ServiceUnavailable(failed.Value, "AI context was not found.")
                    : RpgResult<JsonElement>.NotFound("GameState was not found.");
            }

            var prompt = _promptBuilder.BuildTurnPrompt(context.Value, playerMessage);
            var ai = await _ollama.GenerateAsync(prompt, cancellationToken);
            var parsed = ParseAiResponse(ai.Response);

            var completed = await _turns.CompleteTurnAsync(
                turn,
                parsed.MasterAnswer,
                ai.RawJson,
                ai.Model,
                parsed.Changes,
                cancellationToken);

            return completed.HasValue
                ? RpgResult<JsonElement>.Ok(completed.Value)
                : RpgResult<JsonElement>.NotFound("Turn was not found.");
        }
        catch (OllamaClientException ex)
        {
            var failed = await _turns.FailTurnAsync(turn, ex.Message, null, null, cancellationToken);
            return failed.HasValue
                ? RpgResult<JsonElement>.ServiceUnavailable(failed.Value, ex.Message)
                : RpgResult<JsonElement>.NotFound("Turn was not found.");
        }
        catch (JsonException ex)
        {
            var failed = await _turns.FailTurnAsync(turn, $"AI response JSON was invalid: {ex.Message}", null, null, cancellationToken);
            return failed.HasValue
                ? RpgResult<JsonElement>.ServiceUnavailable(failed.Value, "AI response JSON was invalid.")
                : RpgResult<JsonElement>.NotFound("Turn was not found.");
        }
    }

    public async Task<RpgResult<IReadOnlyList<JsonElement>>> GetTurnsAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        var turns = await _turns.GetTurnsAsync(accountId, gameStateId, cancellationToken);
        return turns is null
            ? RpgResult<IReadOnlyList<JsonElement>>.NotFound("GameState was not found.")
            : RpgResult<IReadOnlyList<JsonElement>>.Ok(turns);
    }

    public async Task<RpgResult<JsonElement>> GetTurnAsync(Guid accountId, Guid gameStateId, Guid turnId, CancellationToken cancellationToken)
    {
        var turn = await _turns.GetTurnAsync(accountId, gameStateId, turnId, cancellationToken);
        return turn.HasValue
            ? RpgResult<JsonElement>.Ok(turn.Value)
            : RpgResult<JsonElement>.NotFound("Turn was not found.");
    }

    private static ParsedAiTurn ParseAiResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return new ParsedAiTurn("Master did not return an answer.", Array.Empty<GameChangeProposal>());
        }

        var trimmed = response.Trim();
        if (!trimmed.StartsWith('{'))
        {
            return new ParsedAiTurn(trimmed, Array.Empty<GameChangeProposal>());
        }

        using var document = JsonDocument.Parse(trimmed);
        var root = document.RootElement;

        var answer = TryGetString(root, "master_answer")
            ?? TryGetString(root, "answer")
            ?? TryGetString(root, "ответ")
            ?? trimmed;

        var changes = new List<GameChangeProposal>();
        if (root.TryGetProperty("changes", out var changesElement) && changesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in changesElement.EnumerateArray())
            {
                var operation = TryGetString(item, "operation") ?? TryGetString(item, "операция");
                if (string.IsNullOrWhiteSpace(operation))
                {
                    continue;
                }

                var payload = item.TryGetProperty("payload", out var payloadElement)
                    ? payloadElement.Clone()
                    : JsonDocument.Parse("{}").RootElement.Clone();

                changes.Add(new GameChangeProposal(operation.Trim(), payload));
            }
        }

        return new ParsedAiTurn(answer, changes);
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        return element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(propertyName, out var property)
            && property.ValueKind == JsonValueKind.String
                ? property.GetString()
                : null;
    }

    private sealed record ParsedAiTurn(string MasterAnswer, IReadOnlyList<GameChangeProposal> Changes);
}
