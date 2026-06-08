using System.Text.Json;
using backend.Infrastructure.Auth;
using backend.Modules.Play.Contracts;
using backend.Shared.Results;

namespace Tests.Services;

public sealed class CoopAccessTests
{
    [Fact]
    public void Player_Can_Play_And_Control_Only_Assigned_Character()
    {
        var accountId = Guid.NewGuid();
        var gameStateId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var characterId = Guid.NewGuid();
        var otherCharacterId = Guid.NewGuid();

        var access = new GameAccess(
            accountId,
            gameStateId,
            ownerId,
            Guid.NewGuid(),
            characterId,
            GameAccessRoles.Player);

        Assert.True(access.CanReadGame);
        Assert.True(access.CanPlayGame);
        Assert.False(access.CanManageGame);
        Assert.False(access.CanViewSecrets);
        Assert.True(access.CanControlCharacter(characterId));
        Assert.False(access.CanControlCharacter(otherCharacterId));
    }

    [Fact]
    public void Observer_Can_Read_But_Not_Mutate()
    {
        var access = new GameAccess(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            GameAccessRoles.Observer);

        Assert.True(access.CanReadGame);
        Assert.False(access.CanPlayGame);
        Assert.False(access.CanManageGame);
        Assert.False(access.CanViewSecrets);
    }

    [Fact]
    public void Host_Can_Manage_And_View_Secrets()
    {
        var accountId = Guid.NewGuid();
        var access = new GameAccess(
            accountId,
            Guid.NewGuid(),
            accountId,
            Guid.NewGuid(),
            null,
            GameAccessRoles.Host);

        Assert.True(access.CanReadGame);
        Assert.True(access.CanPlayGame);
        Assert.True(access.CanManageGame);
        Assert.True(access.CanViewSecrets);
        Assert.True(access.CanControlCharacter(Guid.NewGuid()));
    }

    [Fact]
    public void PlayStateResponse_Serializes_Coop_Permissions_And_Current_Member()
    {
        var memberId = Guid.NewGuid();
        var characterId = Guid.NewGuid();
        var response = EmptyPlayState(JsonSerializer.SerializeToElement(new { ok = true })) with
        {
            Permissions = new PlayPermissionsDto(
                CanRead: true,
                CanPlay: true,
                CanManage: false,
                CanViewSecrets: false,
                CanControlSelectedCharacter: true),
            CurrentPartyMember = new CurrentPartyMemberDto(
                memberId,
                GameAccessRoles.Player,
                characterId,
                IsHost: false)
        };

        var json = JsonSerializer.SerializeToElement(
            response,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        var permissions = json.GetProperty("permissions");
        Assert.True(permissions.GetProperty("canRead").GetBoolean());
        Assert.True(permissions.GetProperty("canPlay").GetBoolean());
        Assert.False(permissions.GetProperty("canManage").GetBoolean());
        Assert.False(permissions.GetProperty("canViewSecrets").GetBoolean());
        Assert.True(permissions.GetProperty("canControlSelectedCharacter").GetBoolean());

        var current = json.GetProperty("currentPartyMember");
        Assert.Equal(memberId, current.GetProperty("id").GetGuid());
        Assert.Equal(GameAccessRoles.Player, current.GetProperty("role").GetString());
        Assert.Equal(characterId, current.GetProperty("characterId").GetGuid());
        Assert.False(current.GetProperty("isHost").GetBoolean());
    }

    [Fact]
    public void SecretRedactor_Removes_Master_And_Debug_Fields_Recursively()
    {
        var gameState = JsonSerializer.SerializeToElement(new
        {
            название = "Тест",
            секретыМастера = "не отдавать игроку",
            nested = new
            {
                hiddenFacts = new[] { "тайна" },
                visible = true
            },
            diagnostics = new
            {
                prompt = "system prompt"
            }
        });

        var response = EmptyPlayState(gameState);
        var redacted = SecretRedactor.Redact(response);

        var value = redacted.GameState!.Value;
        Assert.Equal("Тест", value.GetProperty("название").GetString());
        Assert.True(value.GetProperty("nested").GetProperty("visible").GetBoolean());
        Assert.False(value.TryGetProperty("секретыМастера", out _));
        Assert.False(value.GetProperty("nested").TryGetProperty("hiddenFacts", out _));
        Assert.False(value.TryGetProperty("diagnostics", out _));
    }

    private static PlayStateResponse EmptyPlayState(JsonElement gameState)
        => new(
            "status",
            null,
            gameState,
            null,
            Array.Empty<JsonElement>(),
            Array.Empty<JsonElement>(),
            Array.Empty<JsonElement>(),
            Array.Empty<JsonElement>(),
            null,
            Array.Empty<JsonElement>(),
            Array.Empty<PlayChangeApplicationItem>(),
            Array.Empty<PlayChangeApplicationItem>(),
            Array.Empty<PlayChangeApplicationItem>(),
            null,
            Array.Empty<JsonElement>(),
            Array.Empty<JsonElement>(),
            Array.Empty<JsonElement>(),
            null,
            null,
            null,
            Array.Empty<JsonElement>(),
            null,
            Array.Empty<JsonElement>(),
            DateTimeOffset.UtcNow);
}
