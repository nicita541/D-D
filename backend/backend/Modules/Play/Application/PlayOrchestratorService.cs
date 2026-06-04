using System.Text.Json;
using backend.Shared.Contracts;
using backend.Shared.Kernel;
using backend.Modules.Play.Application;
using backend.Modules.Play.Contracts;
using backend.Modules.Play.Infrastructure;
using backend.Modules.Turns.Contracts;
using backend.Modules.Changes.Application;
using backend.Modules.Changes.Contracts;
using backend.Modules.Changes.Domain;
using backend.Modules.Changes.Infrastructure;
using backend.Modules.Ai.Application;
using backend.Modules.Campaigns.Application;
using backend.Modules.Changes.Application;
using backend.Modules.Characters.Application;
using backend.Modules.Combat.Application;
using backend.Modules.GameStates.Application;
using backend.Modules.Mechanics.Application;
using backend.Modules.Memory.Application;
using backend.Modules.Party.Application;
using backend.Modules.Play.Application;
using backend.Modules.Story.Application;
using backend.Modules.Travel.Application;
using backend.Modules.Turns.Application;
using backend.Modules.World.Application;

namespace backend.Modules.Play.Application;

public sealed class PlayOrchestratorService : IPlayOrchestratorService
{
    private const string DefaultContinueMessage = """
        Продолжи сцену после последнего результата проверки или механического действия.
        Учти последние resolved mechanic requests, броски, pending/applied changes и текущее состояние игры.
        """;

    private readonly IGameStateService _gameStates;
    private readonly ICharacterService _characters;
    private readonly ITurnService _turns;
    private readonly IGameChangeService _changes;
    private readonly IMechanicRequestService _mechanicRequests;
    private readonly IPlayStateService _playState;

    public PlayOrchestratorService(
        IGameStateService gameStates,
        ICharacterService characters,
        ITurnService turns,
        IGameChangeService changes,
        IMechanicRequestService mechanicRequests,
        IPlayStateService playState)
    {
        _gameStates = gameStates;
        _characters = characters;
        _turns = turns;
        _changes = changes;
        _mechanicRequests = mechanicRequests;
        _playState = playState;
    }

    public async Task<RpgResult<PlayStateResponse>> ActAsync(
        Guid accountId,
        Guid gameStateId,
        PlayActRequest request,
        CancellationToken cancellationToken)
    {
        var pendingRequests = await _mechanicRequests.GetRequestsAsync(accountId, gameStateId, "pending", cancellationToken);
        if (pendingRequests.Status == RpgResultStatus.NotFound)
        {
            return RpgResult<PlayStateResponse>.NotFound(pendingRequests.Message ?? "GameState не найден.");
        }

        if (pendingRequests.Status == RpgResultStatus.Ok && pendingRequests.Value?.Count > 0)
        {
            return await _playState.BuildAsync(
                accountId,
                gameStateId,
                new PlayStateBuildRequest(CharacterId: request.ResolvedCharacterId),
                cancellationToken);
        }

        var gameState = await _gameStates.GetGameStateAsync(accountId, gameStateId, cancellationToken);
        if (!gameState.HasValue)
        {
            return RpgResult<PlayStateResponse>.NotFound("GameState не найден.");
        }

        var characters = await _characters.GetCharactersAsync(accountId, gameStateId, cancellationToken);
        if (characters.Count == 0)
        {
            return RpgResult<PlayStateResponse>.BadRequest("Перед действием нужно создать персонажа.");
        }

        var message = BuildActMessage(request);
        if (string.IsNullOrWhiteSpace(message))
        {
            return RpgResult<PlayStateResponse>.BadRequest("Сообщение игрока обязательно.");
        }

        var turn = await _turns.CreateTurnAsync(accountId, gameStateId, new CreateTurnRequest { Message = message }, cancellationToken);
        if (turn.Status != RpgResultStatus.Ok)
        {
            return MapTurnFailure(turn);
        }

        var summary = request.ResolvedAutoApplySafeChanges
            ? await ApplySafeChangesAsync(accountId, gameStateId, cancellationToken)
            : RpgResult<PlayChangeApplicationSummary>.Ok(PlayChangeApplicationSummary.Empty);

        if (summary.Status != RpgResultStatus.Ok)
        {
            return summary.Status switch
            {
                RpgResultStatus.NotFound => RpgResult<PlayStateResponse>.NotFound(summary.Message ?? "GameState не найден."),
                RpgResultStatus.BadRequest => RpgResult<PlayStateResponse>.BadRequest(summary.Message ?? "Не удалось применить safe changes."),
                _ => RpgResult<PlayStateResponse>.ServiceUnavailable(null, summary.Message ?? "Не удалось применить safe changes.")
            };
        }

        var masterAnswer = GetOptionalString(turn.Value, "masterAnswer");
        return await _playState.BuildAsync(
            accountId,
            gameStateId,
            new PlayStateBuildRequest(
                PreferredMode: NormalizePreferredMode(request.ResolvedMode),
                MasterAnswer: masterAnswer,
                CharacterId: request.ResolvedCharacterId,
                ChangeSummary: summary.Value),
            cancellationToken);
    }

    public async Task<RpgResult<PlayStateResponse>> ResolveAndContinueAsync(
        Guid accountId,
        Guid gameStateId,
        Guid requestId,
        PlayResolveAndContinueRequest request,
        CancellationToken cancellationToken)
    {
        var resolve = await _mechanicRequests.ResolveAbilityCheckAsync(
            accountId,
            gameStateId,
            requestId,
            new MechanicRequestResolveAbilityCheckRequest { CharacterId = request.ResolvedCharacterId },
            cancellationToken);

        if (resolve.Status != RpgResultStatus.Ok)
        {
            return resolve.Status switch
            {
                RpgResultStatus.NotFound => RpgResult<PlayStateResponse>.NotFound(resolve.Message ?? "Запрос механики не найден."),
                RpgResultStatus.BadRequest => RpgResult<PlayStateResponse>.BadRequest(resolve.Message ?? "Запрос механики некорректен."),
                RpgResultStatus.Conflict => RpgResult<PlayStateResponse>.Conflict(resolve.Message ?? "Запрос механики конфликтует с текущим состоянием."),
                _ => RpgResult<PlayStateResponse>.ServiceUnavailable(null, resolve.Message ?? "Не удалось выполнить запрос механики.")
            };
        }

        var continueMessage = BuildResolveContinueMessage(request, resolve.Value);
        var turn = await _turns.CreateTurnAsync(accountId, gameStateId, new CreateTurnRequest { Message = continueMessage }, cancellationToken);
        if (turn.Status != RpgResultStatus.Ok)
        {
            return MapTurnFailure(turn);
        }

        var summary = await ApplySafeChangesAsync(accountId, gameStateId, cancellationToken);
        if (summary.Status != RpgResultStatus.Ok)
        {
            return RpgResult<PlayStateResponse>.BadRequest(summary.Message ?? "Не удалось применить safe changes.");
        }

        return await _playState.BuildAsync(
            accountId,
            gameStateId,
            new PlayStateBuildRequest(
                MasterAnswer: GetOptionalString(turn.Value, "masterAnswer"),
                CharacterId: request.ResolvedCharacterId,
                ChangeSummary: summary.Value),
            cancellationToken);
    }

    public async Task<RpgResult<PlayChangeApplicationSummary>> ApplySafeChangesAsync(
        Guid accountId,
        Guid gameStateId,
        CancellationToken cancellationToken)
    {
        var pendingChanges = await _changes.GetChangesAsync(accountId, gameStateId, "pending", cancellationToken);
        if (pendingChanges.Status != RpgResultStatus.Ok)
        {
            return pendingChanges.Status switch
            {
                RpgResultStatus.NotFound => RpgResult<PlayChangeApplicationSummary>.NotFound(pendingChanges.Message ?? "GameState не найден."),
                RpgResultStatus.BadRequest => RpgResult<PlayChangeApplicationSummary>.BadRequest(pendingChanges.Message ?? "Некорректный фильтр changes."),
                _ => RpgResult<PlayChangeApplicationSummary>.ServiceUnavailable(null, pendingChanges.Message ?? "Не удалось получить changes.")
            };
        }

        var applied = new List<PlayChangeApplicationItem>();
        var skipped = new List<PlayChangeApplicationItem>();
        var failed = new List<PlayChangeApplicationItem>();

        foreach (var change in pendingChanges.Value ?? Array.Empty<JsonElement>())
        {
            var changeId = GetOptionalGuid(change, "id");
            var operation = GetOptionalString(change, "operation") ?? string.Empty;
            if (!changeId.HasValue)
            {
                skipped.Add(new PlayChangeApplicationItem(null, operation, null, "Change id отсутствует.", null));
                continue;
            }

            var descriptor = GameChangeOperationPolicy.Describe(operation);
            if (!descriptor.IsSafeAutoApply)
            {
                skipped.Add(new PlayChangeApplicationItem(changeId.Value, operation, null, GetSkipReason(descriptor), null));
                continue;
            }

            var applyResult = await _changes.ApplyChangeAsync(accountId, gameStateId, changeId.Value, cancellationToken);
            if (applyResult.Status == RpgResultStatus.Ok)
            {
                applied.Add(new PlayChangeApplicationItem(changeId.Value, operation, applyResult.Value, null, null));
            }
            else
            {
                failed.Add(new PlayChangeApplicationItem(changeId.Value, operation, null, null, applyResult.Message ?? "Не удалось применить change."));
            }
        }

        return RpgResult<PlayChangeApplicationSummary>.Ok(new PlayChangeApplicationSummary(applied, skipped, failed));
    }

    private static RpgResult<PlayStateResponse> MapTurnFailure(RpgResult<JsonElement> turn)
        => turn.Status switch
        {
            RpgResultStatus.NotFound => RpgResult<PlayStateResponse>.NotFound(turn.Message ?? "GameState не найден."),
            RpgResultStatus.BadRequest => RpgResult<PlayStateResponse>.BadRequest(turn.Message ?? "Ход некорректен."),
            RpgResultStatus.Conflict => RpgResult<PlayStateResponse>.Conflict(turn.Message ?? "В этой игре уже обрабатывается ход."),
            RpgResultStatus.ServiceUnavailable => RpgResult<PlayStateResponse>.ServiceUnavailable(null, turn.Message ?? "AI временно недоступен."),
            _ => RpgResult<PlayStateResponse>.ServiceUnavailable(null, turn.Message ?? "Не удалось создать ход.")
        };

    private static string BuildActMessage(PlayActRequest request)
    {
        var message = request.ResolvedMessage;
        if (string.IsNullOrWhiteSpace(request.ResolvedNote))
        {
            return message;
        }

        return $"{message.Trim()}{Environment.NewLine}{Environment.NewLine}Дополнительная заметка игрока: {request.ResolvedNote}";
    }

    private static string BuildResolveContinueMessage(PlayResolveAndContinueRequest request, JsonElement? resolved)
    {
        var message = DefaultContinueMessage.Trim();
        if (!string.IsNullOrWhiteSpace(request.ResolvedNote))
        {
            message = $"{message}{Environment.NewLine}{Environment.NewLine}Дополнительная заметка игрока: {request.ResolvedNote}";
        }

        if (resolved.HasValue)
        {
            message = $"{message}{Environment.NewLine}{Environment.NewLine}Результат закрытого запроса механики уже сохранён backend. Продолжи сцену с учетом этого результата.";
        }

        return message;
    }

    private static string? NormalizePreferredMode(string mode)
        => string.Equals(mode, "travel", StringComparison.OrdinalIgnoreCase) ? "travel" : null;

    private static string GetSkipReason(GameChangeOperationDescriptor descriptor)
    {
        if (string.IsNullOrWhiteSpace(descriptor.OriginalOperation))
        {
            return "Operation отсутствует.";
        }

        return descriptor.Class switch
        {
            GameChangeOperationClass.Unknown => "unknown operation",
            GameChangeOperationClass.Dangerous => "dangerous operation",
            GameChangeOperationClass.Safe when !descriptor.IsSupported => "unsupported operation",
            GameChangeOperationClass.Unsupported => "unsupported operation",
            _ => "unsupported operation"
        };
    }

    private static string? GetOptionalString(JsonElement? source, string name)
    {
        return source.HasValue
            && source.Value.ValueKind == JsonValueKind.Object
            && source.Value.TryGetProperty(name, out var element)
            && element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;
    }

    private static string? GetOptionalString(JsonElement source, string name)
    {
        return source.ValueKind == JsonValueKind.Object
            && source.TryGetProperty(name, out var element)
            && element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;
    }

    private static Guid? GetOptionalGuid(JsonElement source, string name)
    {
        if (source.ValueKind != JsonValueKind.Object
            || !source.TryGetProperty(name, out var element)
            || element.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return element.ValueKind == JsonValueKind.String && Guid.TryParse(element.GetString(), out var value)
            ? value
            : null;
    }
}
