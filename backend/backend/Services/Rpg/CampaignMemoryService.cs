using System.Text.Json;
using backend.Contracts.Rpg.Common;
using backend.Contracts.Rpg.Memory;
using backend.Repositories.Rpg;

namespace backend.Services.Rpg;

public sealed class CampaignMemoryService : ICampaignMemoryService
{
    private readonly ICampaignMemoryRepository _repository;

    public CampaignMemoryService(ICampaignMemoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<RpgResult<JsonElement>> GetMemoryAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        var memory = await _repository.GetMemoryAsync(accountId, gameStateId, cancellationToken);
        return memory.HasValue
            ? RpgResult<JsonElement>.Ok(memory.Value)
            : RpgResult<JsonElement>.NotFound("GameState не найден.");
    }

    public async Task<RpgResult<JsonElement>> UpdateMemoryAsync(Guid accountId, Guid gameStateId, CampaignMemoryRequest request, CancellationToken cancellationToken)
    {
        try
        {
            Validate(request);
            var memory = await _repository.UpdateMemoryAsync(accountId, gameStateId, request, cancellationToken);
            return memory.HasValue
                ? RpgResult<JsonElement>.Ok(memory.Value)
                : RpgResult<JsonElement>.NotFound("GameState не найден.");
        }
        catch (RpgValidationException ex)
        {
            return RpgResult<JsonElement>.BadRequest(ex.Message);
        }
    }

    private static void Validate(CampaignMemoryRequest request)
    {
        ValidateObject(request.ResolvedCurrentScene, "текущаяСцена");
        ValidateArray(request.ResolvedImportantFacts, "важныеФакты");
        ValidateArray(request.ResolvedOpenThreads, "открытыеЛинии");
        ValidateArray(request.ResolvedResolvedThreads, "закрытыеЛинии");
        ValidateArray(request.ResolvedKnownNpcs, "известныеNpc");
        ValidateArray(request.ResolvedKnownLocations, "известныеЛокации");
        ValidateArray(request.ResolvedMasterSecrets, "секретыМастера");
    }

    private static void ValidateObject(JsonElement? value, string name)
    {
        if (value.HasValue && value.Value.ValueKind is not (JsonValueKind.Object or JsonValueKind.Null))
        {
            throw new RpgValidationException($"{name} должно быть JSON object.");
        }
    }

    private static void ValidateArray(JsonElement? value, string name)
    {
        if (value.HasValue && value.Value.ValueKind is not (JsonValueKind.Array or JsonValueKind.Null))
        {
            throw new RpgValidationException($"{name} должно быть JSON array.");
        }
    }
}
