using backend.Modules.Invites.Contracts;
using backend.Shared.Kernel;

namespace backend.Modules.Invites.Application;

public interface IInviteService
{
    Task<RpgResult<InviteCreatedResponse>> CreateInviteAsync(
        Guid accountId,
        Guid gameStateId,
        CreateInviteRequest request,
        CancellationToken cancellationToken);

    Task<RpgResult<InvitePreviewResponse>> PreviewInviteAsync(string token, CancellationToken cancellationToken);

    Task<RpgResult<InviteAcceptedResponse>> AcceptInviteAsync(
        Guid accountId,
        string token,
        CancellationToken cancellationToken);

    Task<RpgResult<bool>> RevokeInviteAsync(
        Guid accountId,
        Guid gameStateId,
        Guid inviteId,
        CancellationToken cancellationToken);
}
