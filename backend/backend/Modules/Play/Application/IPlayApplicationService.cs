using System.Text.Json;
using backend.Shared.Contracts;
using backend.Shared.Kernel;
using backend.Modules.Mechanics.Contracts;
using backend.Modules.Memory.Contracts;
using backend.Modules.Turns.Contracts;
using backend.Modules.Changes.Application;
using backend.Modules.Changes.Contracts;
using backend.Modules.Changes.Domain;
using backend.Modules.Changes.Infrastructure;

namespace backend.Modules.Play.Application;

public interface IPlayApplicationService
{
    Task<RpgResult<PlayStateResponse>> StatusAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);

    Task<RpgResult<PlayStateResponse>> ActAsync(Guid accountId, Guid gameStateId, PlayActRequest request, CancellationToken cancellationToken);

    Task<RpgResult<JsonElement>> StartAsync(Guid accountId, Guid gameStateId, CreateTurnRequest? request, CancellationToken cancellationToken);

    Task<RpgResult<JsonElement>> MessageAsync(Guid accountId, Guid gameStateId, CreateTurnRequest? request, CancellationToken cancellationToken);

    Task<RpgResult<JsonElement>> ResolveMechanicRequestAsync(
        Guid accountId,
        Guid gameStateId,
        Guid requestId,
        MechanicRequestResolveAbilityCheckRequest? request,
        CancellationToken cancellationToken);

    Task<RpgResult<PlayStateResponse>> ResolveAndContinueAsync(
        Guid accountId,
        Guid gameStateId,
        Guid requestId,
        PlayResolveAndContinueRequest request,
        CancellationToken cancellationToken);

    Task<RpgResult<JsonElement>> ContinueAsync(Guid accountId, Guid gameStateId, PlayContinueRequest? request, CancellationToken cancellationToken);

    Task<RpgResult<JsonElement>> ApplyChangeAsync(Guid accountId, Guid gameStateId, Guid changeId, CancellationToken cancellationToken);

    Task<RpgResult<JsonElement>> RejectChangeAsync(
        Guid accountId,
        Guid gameStateId,
        Guid changeId,
        RejectGameChangeRequest? request,
        CancellationToken cancellationToken);

    Task<RpgResult<PlayChangeApplicationSummary>> ApplySafeChangesAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);

    Task<RpgResult<PlayBootstrapResponse>> BootstrapAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);

    Task<RpgResult<JsonElement>> SummarizeAsync(
        Guid accountId,
        Guid gameStateId,
        CampaignMemorySummarizeRequest? request,
        CancellationToken cancellationToken);
}
