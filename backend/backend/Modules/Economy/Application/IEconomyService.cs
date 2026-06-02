using System.Text.Json;
using backend.Modules.Economy.Contracts;
using backend.Shared.Kernel;

namespace backend.Modules.Economy.Application;

public interface IEconomyService
{
    Task<RpgResult<IReadOnlyList<JsonElement>>> GetLootAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);

    Task<RpgResult<JsonElement>> GetLootContainerAsync(Guid accountId, Guid gameStateId, Guid lootContainerId, CancellationToken cancellationToken);

    Task<RpgResult<JsonElement>> CreateLootAsync(Guid accountId, Guid gameStateId, CreateLootContainerRequest request, CancellationToken cancellationToken);

    Task<RpgResult<JsonElement>> ClaimLootAsync(Guid accountId, Guid gameStateId, Guid lootContainerId, ClaimLootRequest request, CancellationToken cancellationToken);

    Task<RpgResult<JsonElement>> GetCurrencyAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken);

    Task<RpgResult<JsonElement>> AddCurrencyAsync(Guid accountId, Guid gameStateId, Guid characterId, CurrencyChangeRequest request, CancellationToken cancellationToken);

    Task<RpgResult<JsonElement>> SpendCurrencyAsync(Guid accountId, Guid gameStateId, Guid characterId, CurrencyChangeRequest request, CancellationToken cancellationToken);

    Task<RpgResult<JsonElement>> CompleteQuestAsync(Guid accountId, Guid gameStateId, Guid questId, CancellationToken cancellationToken);

    Task<RpgResult<JsonElement>> GrantQuestRewardAsync(Guid accountId, Guid gameStateId, Guid questId, GrantQuestRewardRequest request, CancellationToken cancellationToken);
}
