using System.Text.Json;
using backend.Shared.Contracts;
using backend.Shared.Kernel;
using backend.Modules.Mechanics.Contracts;
using backend.Modules.Ai.Infrastructure;
using backend.Modules.Campaigns.Infrastructure;
using backend.Modules.Changes.Infrastructure;
using backend.Modules.Characters.Infrastructure;
using backend.Modules.Combat.Infrastructure;
using backend.Modules.GameStates.Infrastructure;
using backend.Modules.Mechanics.Infrastructure;
using backend.Modules.Memory.Infrastructure;
using backend.Modules.Party.Infrastructure;
using backend.Modules.Play.Infrastructure;
using backend.Modules.Story.Infrastructure;
using backend.Modules.Travel.Infrastructure;
using backend.Modules.Turns.Infrastructure;
using backend.Modules.World.Infrastructure;

namespace backend.Modules.Mechanics.Application;

public sealed class MechanicRequestService : IMechanicRequestService
{
    private readonly IMechanicRequestRepository _repository;
    private readonly IAbilityCheckService _abilityChecks;

    public MechanicRequestService(IMechanicRequestRepository repository, IAbilityCheckService abilityChecks)
    {
        _repository = repository;
        _abilityChecks = abilityChecks;
    }

    public async Task<RpgResult<IReadOnlyList<JsonElement>>> GetRequestsAsync(Guid accountId, Guid gameStateId, string? status, CancellationToken cancellationToken)
    {
        try
        {
            var requests = await _repository.GetRequestsAsync(accountId, gameStateId, status, cancellationToken);
            return requests is null
                ? RpgResult<IReadOnlyList<JsonElement>>.NotFound("GameState не найден.")
                : RpgResult<IReadOnlyList<JsonElement>>.Ok(requests);
        }
        catch (RpgValidationException ex)
        {
            return RpgResult<IReadOnlyList<JsonElement>>.BadRequest(ex.Message);
        }
    }

    public async Task<RpgResult<JsonElement>> ResolveAbilityCheckAsync(
        Guid accountId,
        Guid gameStateId,
        Guid requestId,
        MechanicRequestResolveAbilityCheckRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var mechanicRequest = await _repository.GetPendingAbilityCheckRequestAsync(accountId, gameStateId, requestId, cancellationToken);
            if (mechanicRequest is null)
            {
                return RpgResult<JsonElement>.NotFound("Запрос механики не найден.");
            }

            var characterId = request.ResolvedCharacterId ?? GetOptionalGuid(mechanicRequest.Payload, "персонажId", "characterId", "character_id");
            if (!characterId.HasValue)
            {
                return RpgResult<JsonElement>.BadRequest("персонажId обязателен для выполнения проверки.");
            }

            var abilityRequest = new AbilityCheckRequest
            {
                CharacterId = characterId.Value,
                Ability = GetRequiredString(mechanicRequest.Payload, "характеристика", "ability"),
                DifficultyClass = GetRequiredInt(mechanicRequest.Payload, "сложность", "difficultyClass"),
                Reason = GetOptionalString(mechanicRequest.Payload, "причина", "reason") ?? string.Empty
            };

            var check = await _abilityChecks.CreateAbilityCheckAsync(accountId, gameStateId, abilityRequest, cancellationToken);
            if (check.Status != RpgResultStatus.Ok)
            {
                return check.Status switch
                {
                    RpgResultStatus.BadRequest => RpgResult<JsonElement>.BadRequest(check.Message ?? "Проверка характеристики некорректна."),
                    RpgResultStatus.NotFound => RpgResult<JsonElement>.NotFound(check.Message ?? "GameState или персонаж не найден."),
                    _ => RpgResult<JsonElement>.ServiceUnavailable(default, check.Message ?? "Не удалось выполнить проверку.")
                };
            }

            var resolved = await _repository.ResolveAsync(accountId, gameStateId, requestId, check.Value, cancellationToken);
            return resolved.HasValue
                ? RpgResult<JsonElement>.Ok(resolved.Value)
                : RpgResult<JsonElement>.NotFound("Запрос механики не найден.");
        }
        catch (RpgValidationException ex)
        {
            return RpgResult<JsonElement>.BadRequest(ex.Message);
        }
    }

    private static Guid? GetOptionalGuid(JsonElement payload, params string[] names)
    {
        var element = GetOptionalElement(payload, names);
        if (!element.HasValue || element.Value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (element.Value.ValueKind == JsonValueKind.String && Guid.TryParse(element.Value.GetString(), out var id))
        {
            return id;
        }

        throw new RpgValidationException($"{names[0]} должен быть UUID.");
    }

    private static string GetRequiredString(JsonElement payload, params string[] names)
    {
        var value = GetOptionalString(payload, names);
        return string.IsNullOrWhiteSpace(value)
            ? throw new RpgValidationException($"{names[0]} обязателен.")
            : value.Trim();
    }

    private static string? GetOptionalString(JsonElement payload, params string[] names)
    {
        var element = GetOptionalElement(payload, names);
        return element.HasValue && element.Value.ValueKind == JsonValueKind.String
            ? element.Value.GetString()
            : null;
    }

    private static int GetRequiredInt(JsonElement payload, params string[] names)
    {
        var element = GetOptionalElement(payload, names);
        if (!element.HasValue || element.Value.ValueKind == JsonValueKind.Null)
        {
            throw new RpgValidationException($"{names[0]} обязательна.");
        }

        return element.Value.ValueKind switch
        {
            JsonValueKind.Number when element.Value.TryGetInt32(out var value) => value,
            JsonValueKind.String when int.TryParse(element.Value.GetString(), out var value) => value,
            _ => throw new RpgValidationException($"{names[0]} должна быть integer.")
        };
    }

    private static JsonElement? GetOptionalElement(JsonElement payload, params string[] names)
    {
        if (payload.ValueKind != JsonValueKind.Object)
        {
            throw new RpgValidationException("payload запроса механики должен быть JSON object.");
        }

        foreach (var name in names)
        {
            if (payload.TryGetProperty(name, out var element))
            {
                return element;
            }
        }

        return null;
    }
}
