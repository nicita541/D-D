using System.Reflection;
using System.Text.Json;
using backend.Controllers;
using backend.Controllers.Rpg;
using backend.Contracts.Auth;
using backend.Contracts.Rpg.Characters;
using backend.Contracts.Rpg.Common;
using backend.Contracts.Rpg.GameStates;
using backend.Infrastructure.Auth;
using backend.Services.Auth;
using backend.Services.Rpg;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tests.Controllers;

public sealed class RpgControllerTests
{
    [Fact]
    public async Task GameStatesController_Create_UsesCurrentAccount()
    {
        var accountId = Guid.NewGuid();
        var createdId = Guid.NewGuid();
        var service = new FakeGameStateService { CreatedId = createdId };
        var controller = new GameStatesController(service, new FakeCurrentUserService(accountId));

        var result = await controller.CreateGameState(new CreateGameStateRequest { Name = "Save" }, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<OperationResponse>(created.Value);
        Assert.Equal(createdId, response.Id);
        Assert.Equal(accountId, service.LastAccountId);
        Assert.Equal("Save", service.LastName);
    }

    [Fact]
    public async Task GameStatesController_Get_ReturnsNotFound_ForMissingOrForeignGameState()
    {
        var controller = new GameStatesController(new FakeGameStateService(), new FakeCurrentUserService(Guid.NewGuid()));

        var result = await controller.GetGameState(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CharactersController_Create_ReturnsNotFound_WhenGameStateIsNotOwned()
    {
        var service = new FakeCharacterService { CreatedId = null };
        var controller = new CharactersController(service, new FakeCurrentUserService(Guid.NewGuid()));

        var result = await controller.CreateCharacter(Guid.NewGuid(), new CreateCharacterRequest { Name = "Hero" }, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task CharactersController_Update_ReturnsOk_WhenCharacterIsOwned()
    {
        var service = new FakeCharacterService { Updated = true };
        var controller = new CharactersController(service, new FakeCurrentUserService(Guid.NewGuid()));
        var characterId = Guid.NewGuid();

        var result = await controller.UpdateCharacter(Guid.NewGuid(), characterId, new UpdateCharacterRequest { Name = "Updated" }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<OperationResponse>(ok.Value);
        Assert.Equal(characterId, response.Id);
    }

    [Fact]
    public void GameControllers_RequireAuthorize()
    {
        var protectedControllers = new[]
        {
            typeof(GameStatesController),
            typeof(CharactersController),
            typeof(StoryController),
            typeof(PartyController),
            typeof(CombatController),
            typeof(AiMasterContextController)
        };

        foreach (var controller in protectedControllers)
        {
            Assert.NotNull(controller.GetCustomAttribute<AuthorizeAttribute>());
        }
    }

    [Fact]
    public void AuthController_PublicAndProtectedActions_AreAnnotated()
    {
        AssertActionHas<AllowAnonymousAttribute>(nameof(AuthController.Register));
        AssertActionHas<AllowAnonymousAttribute>(nameof(AuthController.Login));
        AssertActionHas<AllowAnonymousAttribute>(nameof(AuthController.Refresh));
        AssertActionHas<AuthorizeAttribute>(nameof(AuthController.Logout));
        AssertActionHas<AuthorizeAttribute>(nameof(AuthController.Me));
    }

    [Fact]
    public void CampaignMutations_RequireAuthorize_ButReadsStayPublic()
    {
        AssertActionHas<AuthorizeAttribute>(nameof(CampaignsController.CreateCampaign));
        AssertActionHas<AuthorizeAttribute>(nameof(CampaignsController.DeleteCampaign));
        AssertActionDoesNotHave<AuthorizeAttribute>(nameof(CampaignsController.GetCampaigns));
        AssertActionDoesNotHave<AuthorizeAttribute>(nameof(CampaignsController.GetCampaign));
    }

    private static void AssertActionHas<TAttribute>(string actionName)
        where TAttribute : Attribute
    {
        var method = GetControllerAction(actionName);
        Assert.NotNull(method.GetCustomAttribute<TAttribute>());
    }

    private static void AssertActionDoesNotHave<TAttribute>(string actionName)
        where TAttribute : Attribute
    {
        var method = GetControllerAction(actionName);
        Assert.Null(method.GetCustomAttribute<TAttribute>());
    }

    private static MethodInfo GetControllerAction(string actionName)
    {
        var controllerType = actionName switch
        {
            nameof(AuthController.Register)
                or nameof(AuthController.Login)
                or nameof(AuthController.Refresh)
                or nameof(AuthController.Logout)
                or nameof(AuthController.Me) => typeof(AuthController),
            _ => typeof(CampaignsController)
        };

        return controllerType.GetMethods().Single(method => method.Name == actionName);
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        private readonly Guid _accountId;

        public FakeCurrentUserService(Guid accountId)
        {
            _accountId = accountId;
        }

        public CurrentUser GetRequiredUser()
        {
            return new CurrentUser(_accountId, "hero@example.com", "hero", "user");
        }
    }

    private sealed class FakeGameStateService : IGameStateService
    {
        public Guid CreatedId { get; init; } = Guid.NewGuid();

        public Guid LastAccountId { get; private set; }

        public string? LastName { get; private set; }

        public Task<IReadOnlyList<JsonElement>> GetGameStatesAsync(Guid accountId, CancellationToken cancellationToken)
        {
            LastAccountId = accountId;
            return Task.FromResult<IReadOnlyList<JsonElement>>(Array.Empty<JsonElement>());
        }

        public Task<JsonElement?> GetGameStateAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
        {
            LastAccountId = accountId;
            return Task.FromResult<JsonElement?>(null);
        }

        public Task<Guid> CreateGameStateAsync(Guid accountId, string? name, CancellationToken cancellationToken)
        {
            LastAccountId = accountId;
            LastName = name;
            return Task.FromResult(CreatedId);
        }

        public Task<bool> DeleteGameStateAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
        {
            LastAccountId = accountId;
            return Task.FromResult(false);
        }
    }

    private sealed class FakeCharacterService : ICharacterService
    {
        public Guid? CreatedId { get; init; } = Guid.NewGuid();

        public bool Updated { get; init; }

        public Task<IReadOnlyList<JsonElement>> GetCharactersAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<JsonElement>>(Array.Empty<JsonElement>());

        public Task<JsonElement?> GetCharacterAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
            => Task.FromResult<JsonElement?>(null);

        public Task<Guid?> CreateCharacterAsync(Guid accountId, Guid gameStateId, CreateCharacterRequest request, CancellationToken cancellationToken)
            => Task.FromResult(CreatedId);

        public Task<bool> UpdateCharacterAsync(Guid accountId, Guid gameStateId, Guid characterId, UpdateCharacterRequest request, CancellationToken cancellationToken)
            => Task.FromResult(Updated);

        public Task<bool> DeleteCharacterAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
            => Task.FromResult(false);
    }
}
