using backend.Modules.Invites.Contracts;
using backend.Modules.Invites.Infrastructure;
using backend.Shared.Kernel;

namespace backend.Modules.Invites.Application;

public sealed class InviteService : IInviteService
{
    private readonly IInviteRepository _repository;

    public InviteService(IInviteRepository repository)
    {
        _repository = repository;
    }

    public Task<RpgResult<InviteCreatedResponse>> CreateInviteAsync(
        Guid accountId,
        Guid gameStateId,
        CreateInviteRequest request,
        CancellationToken cancellationToken)
    {
        var role = NormalizeRole(request.ResolvedRole);
        if (role is null)
        {
            return Task.FromResult(RpgResult<InviteCreatedResponse>.BadRequest("Invite role must be player or observer."));
        }

        var token = InviteTokenSecurity.GenerateToken();
        var expiresAt = DateTimeOffset.UtcNow.AddHours(request.ResolvedExpiresInHours);
        return _repository.CreateInviteAsync(
            accountId,
            gameStateId,
            InviteTokenSecurity.HashToken(token),
            token,
            role,
            request.ResolvedMaxUses,
            expiresAt,
            cancellationToken);
    }

    public Task<RpgResult<InvitePreviewResponse>> PreviewInviteAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Task.FromResult(RpgResult<InvitePreviewResponse>.BadRequest("Invite token is required."));
        }

        return _repository.PreviewInviteAsync(InviteTokenSecurity.HashToken(token), cancellationToken);
    }

    public Task<RpgResult<InviteAcceptedResponse>> AcceptInviteAsync(
        Guid accountId,
        string token,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Task.FromResult(RpgResult<InviteAcceptedResponse>.BadRequest("Invite token is required."));
        }

        return _repository.AcceptInviteAsync(accountId, InviteTokenSecurity.HashToken(token), cancellationToken);
    }

    public Task<RpgResult<bool>> RevokeInviteAsync(
        Guid accountId,
        Guid gameStateId,
        Guid inviteId,
        CancellationToken cancellationToken)
        => _repository.RevokeInviteAsync(accountId, gameStateId, inviteId, cancellationToken);

    private static string? NormalizeRole(string role)
    {
        if (string.Equals(role, "player", StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, "игрок", StringComparison.OrdinalIgnoreCase))
        {
            return "player";
        }

        if (string.Equals(role, "observer", StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, "наблюдатель", StringComparison.OrdinalIgnoreCase))
        {
            return "observer";
        }

        return null;
    }
}
