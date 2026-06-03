using System.Text.Json;

namespace backend.Modules.Changes.Application;

public interface IGameChangeHandler
{
    string Operation { get; }

    GameChangeOperationClass Class { get; }

    bool IsSupported { get; }

    Task<JsonElement> ApplyAsync(
        GameChangeContext context,
        JsonElement payload,
        CancellationToken cancellationToken);
}
