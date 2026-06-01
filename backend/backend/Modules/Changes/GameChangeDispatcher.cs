using System.Text.Json;
using backend.Contracts.Rpg.Common;

namespace backend.Modules.Changes;

public sealed class GameChangeDispatcher
{
    private readonly IReadOnlyDictionary<string, IGameChangeHandler> _handlers;

    public GameChangeDispatcher(IEnumerable<IGameChangeHandler> handlers)
    {
        _handlers = handlers
            .GroupBy(handler => handler.Operation, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.First(),
                StringComparer.OrdinalIgnoreCase);
    }

    public async Task<JsonElement> DispatchAsync(
        GameChangeContext context,
        string operation,
        JsonElement payload,
        CancellationToken cancellationToken)
    {
        var descriptor = GameChangeOperationPolicy.Describe(operation);
        if (!descriptor.IsKnown)
        {
            throw new RpgValidationException($"Unknown change operation: {operation}.");
        }

        if (!descriptor.IsSupported)
        {
            throw new RpgValidationException($"Unsupported change operation: {operation}.");
        }

        if (!_handlers.TryGetValue(descriptor.CanonicalOperation, out var handler))
        {
            throw new RpgValidationException($"No change handler registered for operation: {descriptor.CanonicalOperation}.");
        }

        if (!handler.IsSupported)
        {
            throw new RpgValidationException($"Unsupported change operation: {operation}.");
        }

        return await handler.ApplyAsync(context, payload, cancellationToken);
    }
}
