using System.Text.Json;
using backend.Contracts.Rpg.Common;
using backend.Contracts.Rpg.Mechanics;
using backend.Contracts.Rpg.Memory;
using backend.Contracts.Rpg.Turns;
using backend.Modules.Changes;

namespace backend.Modules.Play;

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
