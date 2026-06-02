using System.Text.Json;
using backend.Modules.Economy.Contracts;
using backend.Modules.Economy.Infrastructure;
using backend.Shared.Kernel;

namespace backend.Modules.Economy.Application;

public sealed class EconomyService : IEconomyService
{
    private readonly IEconomyRepository _repository;

    public EconomyService(IEconomyRepository repository)
    {
        _repository = repository;
    }

    public async Task<RpgResult<IReadOnlyList<JsonElement>>> GetLootAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        var result = await _repository.GetLootAsync(accountId, gameStateId, cancellationToken);
        return result is null
            ? RpgResult<IReadOnlyList<JsonElement>>.NotFound("GameState не найден.")
            : RpgResult<IReadOnlyList<JsonElement>>.Ok(result);
    }

    public async Task<RpgResult<JsonElement>> GetLootContainerAsync(Guid accountId, Guid gameStateId, Guid lootContainerId, CancellationToken cancellationToken)
    {
        var result = await _repository.GetLootContainerAsync(accountId, gameStateId, lootContainerId, cancellationToken);
        return result.HasValue
            ? RpgResult<JsonElement>.Ok(result.Value)
            : RpgResult<JsonElement>.NotFound("Добыча не найдена.");
    }

    public async Task<RpgResult<JsonElement>> CreateLootAsync(Guid accountId, Guid gameStateId, CreateLootContainerRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.ResolvedName))
            {
                return RpgResult<JsonElement>.BadRequest("Название добычи обязательно.");
            }

            var result = await _repository.CreateLootAsync(accountId, gameStateId, request, cancellationToken);
            return result.HasValue
                ? RpgResult<JsonElement>.Ok(result.Value)
                : RpgResult<JsonElement>.NotFound("GameState не найден.");
        }
        catch (RpgValidationException ex)
        {
            return RpgResult<JsonElement>.BadRequest(ex.Message);
        }
    }

    public async Task<RpgResult<JsonElement>> ClaimLootAsync(Guid accountId, Guid gameStateId, Guid lootContainerId, ClaimLootRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (!request.ResolvedCharacterId.HasValue)
            {
                return RpgResult<JsonElement>.BadRequest("персонажId обязателен для получения добычи.");
            }

            var result = await _repository.ClaimLootAsync(accountId, gameStateId, lootContainerId, request.ResolvedCharacterId.Value, cancellationToken);
            return result.HasValue
                ? RpgResult<JsonElement>.Ok(result.Value)
                : RpgResult<JsonElement>.NotFound("Добыча или персонаж не найдены.");
        }
        catch (RpgConflictException ex)
        {
            return RpgResult<JsonElement>.Conflict(ex.Message);
        }
        catch (RpgValidationException ex)
        {
            return RpgResult<JsonElement>.BadRequest(ex.Message);
        }
    }

    public async Task<RpgResult<JsonElement>> GetCurrencyAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
    {
        var result = await _repository.GetCurrencyAsync(accountId, gameStateId, characterId, cancellationToken);
        return result.HasValue
            ? RpgResult<JsonElement>.Ok(result.Value)
            : RpgResult<JsonElement>.NotFound("Персонаж не найден.");
    }

    public Task<RpgResult<JsonElement>> AddCurrencyAsync(Guid accountId, Guid gameStateId, Guid characterId, CurrencyChangeRequest request, CancellationToken cancellationToken)
        => ChangeCurrencyAsync(accountId, gameStateId, characterId, request, add: true, cancellationToken);

    public Task<RpgResult<JsonElement>> SpendCurrencyAsync(Guid accountId, Guid gameStateId, Guid characterId, CurrencyChangeRequest request, CancellationToken cancellationToken)
        => ChangeCurrencyAsync(accountId, gameStateId, characterId, request, add: false, cancellationToken);

    public async Task<RpgResult<JsonElement>> CompleteQuestAsync(Guid accountId, Guid gameStateId, Guid questId, CancellationToken cancellationToken)
    {
        var result = await _repository.CompleteQuestAsync(accountId, gameStateId, questId, cancellationToken);
        return result.HasValue
            ? RpgResult<JsonElement>.Ok(result.Value)
            : RpgResult<JsonElement>.NotFound("Квест не найден.");
    }

    public async Task<RpgResult<JsonElement>> GrantQuestRewardAsync(Guid accountId, Guid gameStateId, Guid questId, GrantQuestRewardRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (!request.ResolvedCharacterId.HasValue)
            {
                return RpgResult<JsonElement>.BadRequest("персонажId обязателен для выдачи награды.");
            }

            var result = await _repository.GrantQuestRewardAsync(
                accountId,
                gameStateId,
                questId,
                request.ResolvedCharacterId.Value,
                request.ResolvedXpAmount,
                request.ResolvedCurrencyAmount,
                request.ResolvedItems,
                cancellationToken);

            return result.HasValue
                ? RpgResult<JsonElement>.Ok(result.Value)
                : RpgResult<JsonElement>.NotFound("Квест или персонаж не найдены.");
        }
        catch (RpgConflictException ex)
        {
            return RpgResult<JsonElement>.Conflict(ex.Message);
        }
        catch (RpgValidationException ex)
        {
            return RpgResult<JsonElement>.BadRequest(ex.Message);
        }
    }

    private async Task<RpgResult<JsonElement>> ChangeCurrencyAsync(Guid accountId, Guid gameStateId, Guid characterId, CurrencyChangeRequest request, bool add, CancellationToken cancellationToken)
    {
        try
        {
            if (!request.ResolvedAmount.HasValue || request.ResolvedAmount.Value <= 0)
            {
                return RpgResult<JsonElement>.BadRequest("Сумма должна быть больше 0.");
            }

            var result = add
                ? await _repository.AddCurrencyAsync(accountId, gameStateId, characterId, request.ResolvedAmount.Value, request.ResolvedReason, cancellationToken)
                : await _repository.SpendCurrencyAsync(accountId, gameStateId, characterId, request.ResolvedAmount.Value, request.ResolvedReason, cancellationToken);

            return result.HasValue
                ? RpgResult<JsonElement>.Ok(result.Value)
                : RpgResult<JsonElement>.NotFound("Персонаж не найден.");
        }
        catch (RpgConflictException ex)
        {
            return RpgResult<JsonElement>.Conflict(ex.Message);
        }
        catch (RpgValidationException ex)
        {
            return RpgResult<JsonElement>.BadRequest(ex.Message);
        }
    }
}
