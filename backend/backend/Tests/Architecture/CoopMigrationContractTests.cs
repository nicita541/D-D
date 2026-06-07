namespace Tests.Architecture;

public sealed class CoopMigrationContractTests
{
    private static readonly string MigrationSql = File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "database", "init", "014_coop_invites_and_access.sql"));

    [Fact]
    public void Coop_Migration_Creates_Invite_Tables_And_Token_Hash_Index()
    {
        Assert.Contains("CREATE TABLE IF NOT EXISTS game.invites", MigrationSql);
        Assert.Contains("token_hash", MigrationSql);
        Assert.Contains("CREATE TABLE IF NOT EXISTS game.invite_acceptances", MigrationSql);
        Assert.Contains("ix_invites_token_hash", MigrationSql);
    }

    [Fact]
    public void Coop_Migration_Backfills_Host_Membership_Idempotently()
    {
        Assert.Contains("game.ensure_host_party", MigrationSql);
        Assert.Contains("INSERT INTO game.party_members", MigrationSql);
        Assert.Contains("ON CONFLICT (game_state_id) DO NOTHING", MigrationSql);
        Assert.Contains("AND NOT EXISTS (", MigrationSql);
        Assert.Contains("ux_party_members_active_account", MigrationSql);
    }

    [Fact]
    public void Coop_Migration_Does_Not_Rewrite_User_Or_Ai_Text()
    {
        Assert.DoesNotContain("game.turns", MigrationSql);
        Assert.DoesNotContain("game.memories", MigrationSql);
        Assert.DoesNotContain("regexp_replace", MigrationSql);
    }
}
