using System.Text.Json;
using backend.Modules.Economy.Contracts;

namespace backend.Modules.Economy.Infrastructure;

public interface IEconomyRepository
{
    Task<IReadOnlyList<JsonElement>?> GetLootAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);

    Task<JsonElement?> GetLootContainerAsync(Guid accountId, Guid gameStateId, Guid lootContainerId, CancellationToken cancellationToken);

    Task<JsonElement?> CreateLootAsync(Guid accountId, Guid gameStateId, CreateLootContainerRequest request, CancellationToken cancellationToken);

    Task<JsonElement?> ClaimLootAsync(Guid accountId, Guid gameStateId, Guid lootContainerId, Guid characterId, CancellationToken cancellationToken);

    Task<JsonElement?> GetCurrencyAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken);

    Task<JsonElement?> AddCurrencyAsync(Guid accountId, Guid gameStateId, Guid characterId, int amount, string reason, CancellationToken cancellationToken);

    Task<JsonElement?> SpendCurrencyAsync(Guid accountId, Guid gameStateId, Guid characterId, int amount, string reason, CancellationToken cancellationToken);

    Task<JsonElement?> CompleteQuestAsync(Guid accountId, Guid gameStateId, Guid questId, CancellationToken cancellationToken);

    Task<JsonElement?> GrantQuestRewardAsync(Guid accountId, Guid gameStateId, Guid questId, Guid characterId, int xpAmount, int currencyAmount, IReadOnlyList<CreateLootItemRequest> items, CancellationToken cancellationToken);
}
