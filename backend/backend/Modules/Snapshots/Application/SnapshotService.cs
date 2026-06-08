using backend.Modules.Snapshots.Contracts;
using backend.Modules.Snapshots.Infrastructure;
using backend.Shared.Kernel;

namespace backend.Modules.Snapshots.Application;

public sealed class SnapshotService : ISnapshotService
{
    private readonly ISnapshotRepository _repository;

    public SnapshotService(ISnapshotRepository repository)
    {
        _repository = repository;
    }

    public Task<RpgResult<IReadOnlyList<SnapshotDto>>> ListSnapshotsAsync(Guid ownerAccountId, Guid gameStateId, CancellationToken cancellationToken)
        => _repository.ListSnapshotsAsync(ownerAccountId, gameStateId, cancellationToken);

    public Task<RpgResult<SnapshotDto>> CreateSnapshotAsync(
        Guid ownerAccountId,
        Guid gameStateId,
        Guid createdByAccountId,
        string reason,
        CancellationToken cancellationToken)
        => _repository.CreateSnapshotAsync(ownerAccountId, gameStateId, createdByAccountId, NormalizeReason(reason), cancellationToken);

    public Task<RpgResult<SnapshotRestoreResponse>> RestoreSnapshotAsync(
        Guid ownerAccountId,
        Guid gameStateId,
        Guid snapshotId,
        Guid restoredByAccountId,
        CancellationToken cancellationToken)
        => _repository.RestoreSnapshotAsync(ownerAccountId, gameStateId, snapshotId, restoredByAccountId, cancellationToken);

    private static string NormalizeReason(string? reason)
        => string.IsNullOrWhiteSpace(reason) ? "Ручной снимок." : reason.Trim();
}
