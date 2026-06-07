using backend.Modules.Invites.Contracts;
using backend.Shared.Kernel;

namespace backend.Modules.Invites.Infrastructure;

public interface IInviteRepository
{
    Task<RpgResult<InviteCreatedResponse>> CreateInviteAsync(
        Guid accountId,
        Guid gameStateId,
        string tokenHash,
        string rawToken,
        string role,
        int maxUses,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken);

    Task<RpgResult<InvitePreviewResponse>> PreviewInviteAsync(string tokenHash, CancellationToken cancellationToken);

    Task<RpgResult<InviteAcceptedResponse>> AcceptInviteAsync(
        Guid accountId,
        string tokenHash,
        CancellationToken cancellationToken);

    Task<RpgResult<bool>> RevokeInviteAsync(
        Guid accountId,
        Guid gameStateId,
        Guid inviteId,
        CancellationToken cancellationToken);
}
