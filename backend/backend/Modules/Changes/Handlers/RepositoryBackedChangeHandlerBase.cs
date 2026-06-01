using System.Text.Json;

namespace backend.Modules.Changes;

public abstract class RepositoryBackedChangeHandlerBase : IGameChangeHandler
{
    protected RepositoryBackedChangeHandlerBase(string operation, GameChangeOperationClass operationClass, bool isSupported)
    {
        Operation = operation;
        Class = operationClass;
        IsSupported = isSupported;
    }

    public string Operation { get; }

    public GameChangeOperationClass Class { get; }

    public bool IsSupported { get; }

    public Task<JsonElement> ApplyAsync(
        GameChangeContext context,
        JsonElement payload,
        CancellationToken cancellationToken)
        => context.ApplyCanonicalOperationAsync(Operation, payload, cancellationToken);
}
