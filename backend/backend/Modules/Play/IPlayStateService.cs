using backend.Contracts.Rpg.Common;
using backend.Modules.Play;

namespace backend.Modules.Play;

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
