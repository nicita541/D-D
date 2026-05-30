using backend.Contracts.Rpg.Common;
using backend.Contracts.Rpg.Play;

namespace backend.Services.Rpg;

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
