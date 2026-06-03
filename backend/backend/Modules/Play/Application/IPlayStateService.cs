using backend.Shared.Contracts;
using backend.Shared.Kernel;
using backend.Modules.Play.Application;
using backend.Modules.Play.Contracts;
using backend.Modules.Play.Infrastructure;

namespace backend.Modules.Play.Application;

public sealed record PlayStateBuildRequest(
    string? PreferredMode = null,
    string? MasterAnswer = null,
    Guid? CharacterId = null,
    PlayChangeApplicationSummary? ChangeSummary = null);

public interface IPlayStateService
{
    Task<RpgResult<PlayStateResponse>> BuildAsync(
        Guid accountId,
        Guid gameStateId,
        PlayStateBuildRequest request,
        CancellationToken cancellationToken);
}
