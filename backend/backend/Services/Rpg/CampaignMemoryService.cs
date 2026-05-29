using System.Text.Json;
using backend.Contracts.Rpg.Common;
using backend.Contracts.Rpg.Memory;
using backend.Infrastructure.Ai;
using backend.Repositories.Rpg;

namespace backend.Services.Rpg;

public sealed class CampaignMemoryService : ICampaignMemoryService
{
    private readonly ICampaignMemoryRepository _repository;
    private readonly IOllamaClient? _ollama;

    public CampaignMemoryService(ICampaignMemoryRepository repository)
        : this(repository, null)
    {
    }

    public CampaignMemoryService(ICampaignMemoryRepository repository, IOllamaClient? ollama)
    {
        _repository = repository;
        _ollama = ollama;
    }

    public async Task<RpgResult<JsonElement>> SummarizeMemoryAsync(Guid accountId, Guid gameStateId, CampaignMemorySummarizeRequest request, CancellationToken cancellationToken)
    {
        if (_ollama is null)
        {
            return RpgResult<JsonElement>.ServiceUnavailable(default, "AI-клиент недоступен.");
        }

        var limit = Math.Clamp(request.ResolvedRecentEntries, 1, 100);
        var entries = await _repository.GetRecentLogEntriesAsync(accountId, gameStateId, limit, cancellationToken);
        if (entries is null)
        {
            return RpgResult<JsonElement>.NotFound("GameState не найден.");
        }

        if (entries.Count == 0)
        {
            return RpgResult<JsonElement>.BadRequest("Нет записей журнала для суммаризации.");
        }

        try
        {
            var prompt = BuildSummarizationPrompt(entries);
            var ai = await _ollama.GenerateAsync(prompt, cancellationToken);
            var payload = ParseMemoryPatch(ai.Response);
            var memory = await _repository.ApplyMemoryPatchAsync(accountId, gameStateId, payload, cancellationToken);
            return memory.HasValue
                ? RpgResult<JsonElement>.Ok(memory.Value)
                : RpgResult<JsonElement>.NotFound("GameState не найден.");
        }
        catch (OllamaClientException)
        {
            return RpgResult<JsonElement>.ServiceUnavailable(default, "AI-сервис недоступен для суммаризации памяти.");
        }
        catch (JsonException)
        {
            return RpgResult<JsonElement>.ServiceUnavailable(default, "AI вернул невалидный JSON для памяти кампании.");
        }
        catch (RpgValidationException ex)
        {
            return RpgResult<JsonElement>.ServiceUnavailable(default, $"AI вернул некорректное обновление памяти: {ex.Message}");
        }
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

    private static string BuildSummarizationPrompt(IReadOnlyList<JsonElement> entries)
    {
        var journalJson = JsonSerializer.Serialize(entries.Select(entry => new
        {
            turnNumber = entry.GetProperty("turnNumber").GetInt32(),
            type = entry.GetProperty("type").GetString(),
            text = entry.GetProperty("text").GetString(),
            important = entry.GetProperty("important").GetBoolean()
        }));

        return $$"""
            Ты русскоязычный RPG-мастер и редактор долгой памяти кампании.
            На основе последних записей журнала верни только валидный JSON object без markdown и без ```json.
            Не раскрывай секреты мастера в player-facing summary. Поле masterSecretsAdd используй только для GM-only секретов.

            Схема ответа:
            {
              "summaryAppend": "краткое резюме новых важных событий на русском",
              "currentScene": {},
              "importantFactsAdd": [],
              "openThreadsAdd": [],
              "resolvedThreadsAdd": [],
              "knownNpcsUpsert": [],
              "knownLocationsUpsert": [],
              "masterSecretsAdd": []
            }

            Записи журнала:
            {{journalJson}}
            """;
    }

    private static JsonElement ParseMemoryPatch(string response)
    {
        using var document = JsonDocument.Parse(response);
        var root = document.RootElement.Clone();
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new RpgValidationException("ответ должен быть JSON object.");
        }

        _ = CampaignMemoryMergeHelper.Merge(CreateEmptyMemory(), root);
        return root;
    }

    private static JsonElement CreateEmptyMemory()
        => JsonSerializer.SerializeToElement(new
        {
            резюме = string.Empty,
            текущаяСцена = new { },
            важныеФакты = Array.Empty<object>(),
            открытыеЛинии = Array.Empty<object>(),
            закрытыеЛинии = Array.Empty<object>(),
            известныеNpc = Array.Empty<object>(),
            известныеЛокации = Array.Empty<object>(),
            секретыМастера = Array.Empty<object>()
        });

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
