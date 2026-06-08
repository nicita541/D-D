namespace Tests.Architecture;

public sealed class SnapshotMigrationContractTests
{
    private static readonly string MigrationSql = File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "database", "init", "015_snapshots.sql"));

    [Fact]
    public void Snapshot_Migration_Creates_Snapshot_Table_And_Indexes()
    {
        Assert.Contains("CREATE TABLE IF NOT EXISTS game.snapshots", MigrationSql);
        Assert.Contains("payload jsonb NOT NULL", MigrationSql);
        Assert.Contains("ix_snapshots_game_state_id_created_at", MigrationSql);
        Assert.Contains("ix_snapshots_created_by_account_id", MigrationSql);
    }

    [Fact]
    public void Snapshot_Migration_Is_Idempotent_And_Does_Not_Rewrite_Game_Text()
    {
        Assert.Contains("CREATE TABLE IF NOT EXISTS", MigrationSql);
        Assert.Contains("CREATE INDEX IF NOT EXISTS", MigrationSql);
        Assert.DoesNotContain("UPDATE game.turns", MigrationSql);
        Assert.DoesNotContain("UPDATE game.memories", MigrationSql);
        Assert.DoesNotContain("regexp_replace", MigrationSql);
    }
}
