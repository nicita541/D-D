using System.Text.Json;
using backend.Contracts.Rpg.Common;
using backend.Contracts.Rpg.Turns;
using backend.Infrastructure.Ai;
using backend.Repositories.Rpg;

namespace backend.Services.Rpg;

public sealed class TurnService : ITurnService
{
    private const int PlayerMessageMaxLength = 4000;
    private const int MasterAnswerMaxLength = 12000;
    private const int MaxChangesPerResponse = 10;
    private const int RawAiResponseMaxLength = 60000;
    private const int DiagnosticFieldMaxLength = 20000;

    private static readonly HashSet<string> AllowedOperations = new(StringComparer.Ordinal)
    {
        "добавить_предмет",
        "изменить_хп",
        "изменить_ресурс",
        "добавить_состояние",
        "удалить_состояние",
        "обновить_квест",
        "добавить_запись_журнала",
        "переместить_предмет",
        "запросить_бросок",
        "обновить_память"
    };

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
            return RpgResult<JsonElement>.BadRequest("Сообщение игрока обязательно.");
        }

        if (playerMessage.Length > PlayerMessageMaxLength)
        {
            return RpgResult<JsonElement>.BadRequest($"Сообщение игрока не должно превышать {PlayerMessageMaxLength} символов.");
        }

        var creation = await _turns.CreatePendingTurnAsync(accountId, gameStateId, playerMessage, cancellationToken);
        if (creation.Status == PendingTurnCreationStatus.NotFound)
        {
            return RpgResult<JsonElement>.NotFound("GameState не найден.");
        }

        if (creation.Status == PendingTurnCreationStatus.Conflict)
        {
            return RpgResult<JsonElement>.Conflict("В этой игре уже обрабатывается ход.");
        }

        var turn = creation.Turn
            ?? throw new InvalidOperationException("Pending turn was not returned.");

        try
        {
            var context = await _context.GetContextAsync(accountId, gameStateId, recentEventsLimit: 20, cancellationToken);
            if (!context.HasValue)
            {
                const string message = "AI context не найден.";
                var failed = await _turns.FailTurnAsync(turn, message, null, null, cancellationToken);
                return failed.HasValue
                    ? RpgResult<JsonElement>.ServiceUnavailable(failed.Value, message)
                    : RpgResult<JsonElement>.NotFound("GameState не найден.");
            }

            var prompt = _promptBuilder.BuildTurnPrompt(context.Value, playerMessage);
            var firstAi = await _ollama.GenerateAsync(prompt, cancellationToken);
            var parsed = ValidateAiResponse(firstAi.Response);
            OllamaGenerateResult finalAi = firstAi;
            string rawAiResponse;

            if (!parsed.IsValid)
            {
                var firstError = parsed.ErrorMessage;
                var repairPrompt = _promptBuilder.BuildRepairPrompt(firstAi.Response, firstError);
                try
                {
                    var retryAi = await _ollama.GenerateAsync(repairPrompt, cancellationToken);
                    var retryParsed = ValidateAiResponse(retryAi.Response);
                    if (!retryParsed.IsValid)
                    {
                        var errorMessage = $"AI вернул невалидный JSON после повторной попытки: {retryParsed.ErrorMessage}";
                        var failed = await _turns.FailTurnAsync(
                            turn,
                            errorMessage,
                            BuildRawAiDiagnostic(firstAi, firstError, retryAi, retryParsed.ErrorMessage),
                            retryAi.Model,
                            cancellationToken);

                        return failed.HasValue
                            ? RpgResult<JsonElement>.ServiceUnavailable(failed.Value, errorMessage)
                            : RpgResult<JsonElement>.NotFound("Turn не найден.");
                    }

                    parsed = retryParsed;
                    finalAi = retryAi;
                    rawAiResponse = BuildRawAiDiagnostic(firstAi, firstError, retryAi, null);
                }
                catch (OllamaClientException ex)
                {
                    var errorMessage = $"Повторный запрос к AI не удался: {ex.Message}";
                    var failed = await _turns.FailTurnAsync(
                        turn,
                        errorMessage,
                        BuildRawAiDiagnostic(firstAi, firstError, null, ex.Message),
                        firstAi.Model,
                        cancellationToken);

                    return failed.HasValue
                        ? RpgResult<JsonElement>.ServiceUnavailable(failed.Value, errorMessage)
                        : RpgResult<JsonElement>.NotFound("Turn не найден.");
                }
            }
            else
            {
                rawAiResponse = BuildRawAiResponseJson(firstAi.RawJson);
            }

            var completed = await _turns.CompleteTurnAsync(
                turn,
                parsed.MasterAnswer,
                rawAiResponse,
                finalAi.Model,
                parsed.Changes,
                cancellationToken);

            return completed.HasValue
                ? RpgResult<JsonElement>.Ok(completed.Value)
                : RpgResult<JsonElement>.NotFound("Turn не найден.");
        }
        catch (OllamaClientException ex)
        {
            var failed = await _turns.FailTurnAsync(turn, ex.Message, null, null, cancellationToken);
            return failed.HasValue
                ? RpgResult<JsonElement>.ServiceUnavailable(failed.Value, ex.Message)
                : RpgResult<JsonElement>.NotFound("Turn не найден.");
        }
    }

    public async Task<RpgResult<IReadOnlyList<JsonElement>>> GetTurnsAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        var turns = await _turns.GetTurnsAsync(accountId, gameStateId, cancellationToken);
        return turns is null
            ? RpgResult<IReadOnlyList<JsonElement>>.NotFound("GameState не найден.")
            : RpgResult<IReadOnlyList<JsonElement>>.Ok(turns);
    }

    public async Task<RpgResult<JsonElement>> GetTurnAsync(Guid accountId, Guid gameStateId, Guid turnId, CancellationToken cancellationToken)
    {
        var turn = await _turns.GetTurnAsync(accountId, gameStateId, turnId, cancellationToken);
        return turn.HasValue
            ? RpgResult<JsonElement>.Ok(turn.Value)
            : RpgResult<JsonElement>.NotFound("Turn не найден.");
    }

    private static ParsedAiTurn ValidateAiResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return ParsedAiTurn.Invalid("AI вернул пустой ответ.");
        }

        var trimmed = response.Trim();
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(trimmed);
        }
        catch (JsonException ex)
        {
            return ParsedAiTurn.Invalid($"AI response должен быть валидным JSON object: {ex.Message}");
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return ParsedAiTurn.Invalid("AI response должен быть JSON object.");
            }

            if (!root.TryGetProperty("master_answer", out var answerElement))
            {
                return ParsedAiTurn.Invalid("AI response должен содержать поле master_answer.");
            }

            if (answerElement.ValueKind != JsonValueKind.String)
            {
                return ParsedAiTurn.Invalid("Поле master_answer должно быть строкой.");
            }

            var answer = answerElement.GetString()?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(answer))
            {
                return ParsedAiTurn.Invalid("Поле master_answer не должно быть пустым.");
            }

            if (answer.Length > MasterAnswerMaxLength)
            {
                return ParsedAiTurn.Invalid($"Поле master_answer не должно превышать {MasterAnswerMaxLength} символов.");
            }

            if (!root.TryGetProperty("changes", out var changesElement))
            {
                return ParsedAiTurn.Invalid("AI response должен содержать поле changes.");
            }

            if (changesElement.ValueKind != JsonValueKind.Array)
            {
                return ParsedAiTurn.Invalid("Поле changes должно быть массивом.");
            }

            var changes = new List<GameChangeProposal>();
            foreach (var item in changesElement.EnumerateArray())
            {
                if (changes.Count >= MaxChangesPerResponse)
                {
                    return ParsedAiTurn.Invalid($"AI response не должен содержать больше {MaxChangesPerResponse} изменений.");
                }

                if (item.ValueKind != JsonValueKind.Object)
                {
                    return ParsedAiTurn.Invalid("Каждый элемент changes должен быть объектом.");
                }

                if (!item.TryGetProperty("operation", out var operationElement))
                {
                    return ParsedAiTurn.Invalid("Каждый change должен содержать поле operation.");
                }

                if (operationElement.ValueKind != JsonValueKind.String)
                {
                    return ParsedAiTurn.Invalid("Поле operation должно быть строкой.");
                }

                var operation = operationElement.GetString()?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(operation))
                {
                    return ParsedAiTurn.Invalid("Поле operation не должно быть пустым.");
                }

                if (!AllowedOperations.Contains(operation))
                {
                    return ParsedAiTurn.Invalid($"Операция '{operation}' не поддерживается.");
                }

                if (!item.TryGetProperty("payload", out var payloadElement))
                {
                    return ParsedAiTurn.Invalid("Каждый change должен содержать поле payload.");
                }

                if (payloadElement.ValueKind != JsonValueKind.Object)
                {
                    return ParsedAiTurn.Invalid("Поле payload должно быть объектом.");
                }

                var payload = payloadElement.Clone();
                changes.Add(new GameChangeProposal(operation.Trim(), payload));
            }

            return ParsedAiTurn.Valid(answer, changes);
        }
    }

    private static string BuildRawAiResponseJson(string rawJson)
    {
        if (rawJson.Length <= RawAiResponseMaxLength)
        {
            return rawJson;
        }

        return JsonSerializer.Serialize(new
        {
            truncated = true,
            originalLength = rawJson.Length,
            excerpt = rawJson[..RawAiResponseMaxLength]
        });
    }

    private static string BuildRawAiDiagnostic(
        OllamaGenerateResult first,
        string? firstError,
        OllamaGenerateResult? retry,
        string? retryError)
    {
        return JsonSerializer.Serialize(new
        {
            attempts = new[]
            {
                new
                {
                    name = "first",
                    model = first.Model,
                    validationError = firstError,
                    rawJson = Snapshot(first.RawJson),
                    response = Snapshot(first.Response)
                },
                retry is null
                    ? null
                    : new
                    {
                        name = "retry",
                        model = retry.Model,
                        validationError = retryError,
                        rawJson = Snapshot(retry.RawJson),
                        response = Snapshot(retry.Response)
                    }
            }.Where(attempt => attempt is not null)
        });
    }

    private static object Snapshot(string value)
    {
        return value.Length <= DiagnosticFieldMaxLength
            ? new { truncated = false, originalLength = value.Length, value }
            : new { truncated = true, originalLength = value.Length, value = value[..DiagnosticFieldMaxLength] };
    }

    private sealed record ParsedAiTurn(
        bool IsValid,
        string MasterAnswer,
        IReadOnlyList<GameChangeProposal> Changes,
        string ErrorMessage)
    {
        public static ParsedAiTurn Valid(string masterAnswer, IReadOnlyList<GameChangeProposal> changes)
            => new(true, masterAnswer, changes, string.Empty);

        public static ParsedAiTurn Invalid(string errorMessage)
            => new(false, string.Empty, Array.Empty<GameChangeProposal>(), errorMessage);
    }
}
