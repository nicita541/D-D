using backend.Modules.Snapshots.Contracts;
using backend.Shared.Kernel;

namespace backend.Modules.Snapshots.Infrastructure;

public interface ISnapshotRepository
{
    Task<RpgResult<IReadOnlyList<SnapshotDto>>> ListSnapshotsAsync(Guid ownerAccountId, Guid gameStateId, CancellationToken cancellationToken);

    Task<RpgResult<SnapshotDto>> CreateSnapshotAsync(
        Guid ownerAccountId,
        Guid gameStateId,
        Guid createdByAccountId,
        string reason,
        CancellationToken cancellationToken);

    Task<RpgResult<SnapshotRestoreResponse>> RestoreSnapshotAsync(
        Guid ownerAccountId,
        Guid gameStateId,
        Guid snapshotId,
        Guid restoredByAccountId,
        CancellationToken cancellationToken);
}
