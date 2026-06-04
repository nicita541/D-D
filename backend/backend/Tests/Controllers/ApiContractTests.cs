using System.Reflection;
using System.Text.Json;
using backend.Infrastructure.Api;
using backend.Modules.Ai.Api;
using backend.Modules.Campaigns.Api;
using backend.Modules.Changes.Api;
using backend.Modules.Characters.Api;
using backend.Modules.Combat.Api;
using backend.Modules.GameStates.Api;
using backend.Modules.Mechanics.Api;
using backend.Modules.Memory.Api;
using backend.Modules.Party.Api;
using backend.Modules.Play.Api;
using backend.Modules.Story.Api;
using backend.Modules.Travel.Api;
using backend.Modules.Turns.Api;
using backend.Modules.World.Api;
using backend.Infrastructure.Auth;
using backend.Modules.Changes.Application;
using backend.Modules.Changes.Contracts;
using backend.Modules.Changes.Domain;
using backend.Modules.Changes.Infrastructure;
using backend.Modules.Combat.Application;
using backend.Modules.Combat.Contracts;
using backend.Modules.Combat.Domain;
using backend.Modules.Combat.Infrastructure;
using backend.Modules.Ai.Application;
using backend.Modules.Campaigns.Application;
using backend.Modules.Characters.Application;
using backend.Modules.GameStates.Application;
using backend.Modules.Mechanics.Application;
using backend.Modules.Memory.Application;
using backend.Modules.Party.Application;
using backend.Modules.Play.Application;
using backend.Modules.Story.Application;
using backend.Modules.Travel.Application;
using backend.Modules.Turns.Application;
using backend.Modules.World.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tests.Controllers;

public sealed class ApiContractTests
{
    [Fact]
    public void ProtectedRpgControllers_HaveAuthorize_ApiController_AndApiRoute()
    {
        var protectedControllers = new[]
        {
            typeof(GameStatesController),
            typeof(CharactersController),
            typeof(StoryController),
            typeof(PartyController),
            typeof(CombatController),
            typeof(AiMasterContextController),
            typeof(CharacterDomainController),
            typeof(TurnsController),
            typeof(GameChangesController),
            typeof(WorldController),
            typeof(RollsController),
            typeof(ChecksController),
            typeof(CampaignMemoryController),
            typeof(MechanicRequestsController),
            typeof(CharacterProgressionController)
        };

        foreach (var controller in protectedControllers)
        {
            Assert.NotNull(controller.GetCustomAttribute<AuthorizeAttribute>());
            Assert.NotNull(controller.GetCustomAttribute<ApiControllerAttribute>());

            var route = controller.GetCustomAttribute<RouteAttribute>();
            Assert.NotNull(route);
            Assert.False(string.IsNullOrWhiteSpace(route!.Template));
            Assert.StartsWith("api/", route.Template);
        }
    }

    [Fact]
    public void PublicControllers_KeepExpectedAuthAnnotations()
    {
        Assert.NotNull(typeof(HealthController).GetCustomAttribute<AllowAnonymousAttribute>());

        AssertActionHas<AllowAnonymousAttribute>(typeof(AuthController), nameof(AuthController.Register));
        AssertActionHas<AllowAnonymousAttribute>(typeof(AuthController), nameof(AuthController.Login));
        AssertActionHas<AllowAnonymousAttribute>(typeof(AuthController), nameof(AuthController.Refresh));

        AssertActionHas<AuthorizeAttribute>(typeof(AuthController), nameof(AuthController.Me));
        AssertActionHas<AuthorizeAttribute>(typeof(AuthController), nameof(AuthController.Logout));
    }

    [Fact]
    public void AiContextController_Route_And_Method_AreStable()
    {
        var route = typeof(AiMasterContextController).GetCustomAttribute<RouteAttribute>();

        Assert.NotNull(route);
        Assert.Equal("api/game-states/{gameStateId:guid}/ai-context", route!.Template);

        var action = typeof(AiMasterContextController).GetMethod(nameof(AiMasterContextController.GetContext));
        Assert.NotNull(action);
        Assert.NotNull(action!.GetCustomAttribute<HttpGetAttribute>());
    }

    [Fact]
    public async Task AiContextController_GetContext_ReturnsOk_WithMechanicsFields()
    {
        var accountId = Guid.NewGuid();
        var gameStateId = Guid.NewGuid();

        using var document = JsonDocument.Parse("""
        {
          "gameStateId": "00000000-0000-0000-0000-000000000001",
          "номерхода": 1,
          "режим": "exploration",
          "сюжет": {},
          "партия": {},
          "персонажи": [],
          "мир": {},
          "квесты": [],
          "последниеСобытия": [],
          "последниеБроски": [],
          "последниеПроверки": [],
          "запросыМеханик": {
            "pending": [],
            "resolved": []
          },
          "памятьКампании": {
            "резюме": "",
            "текущаяСцена": {},
            "важныеФакты": [],
            "открытыеЛинии": [],
            "закрытыеЛинии": [],
            "известныеNpc": [],
            "известныеЛокации": [],
            "секретыМастера": []
          },
          "бой": {}
        }
        """);

        var context = document.RootElement.Clone();
        var service = new FakeAiMasterContextService(context);
        var currentUser = new FakeCurrentUserService(accountId);
        var controller = new AiMasterContextController(service, currentUser);

        var result = await controller.GetContext(gameStateId, 25, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var value = Assert.IsType<JsonElement>(ok.Value);

        Assert.Equal(accountId, service.LastAccountId);
        Assert.Equal(gameStateId, service.LastGameStateId);
        Assert.Equal(25, service.LastRecentEventsLimit);

        Assert.True(value.TryGetProperty("персонажи", out _));
        Assert.True(value.TryGetProperty("последниеБроски", out var rolls));
        Assert.True(value.TryGetProperty("последниеПроверки", out var checks));
        Assert.True(value.TryGetProperty("запросыМеханик", out var requests));
        Assert.True(value.TryGetProperty("памятьКампании", out _));

        Assert.Equal(JsonValueKind.Array, rolls.ValueKind);
        Assert.Equal(JsonValueKind.Array, checks.ValueKind);
        Assert.Equal(JsonValueKind.Object, requests.ValueKind);
        Assert.True(requests.TryGetProperty("pending", out var pending));
        Assert.True(requests.TryGetProperty("resolved", out var resolved));
        Assert.Equal(JsonValueKind.Array, pending.ValueKind);
        Assert.Equal(JsonValueKind.Array, resolved.ValueKind);
    }

    [Fact]
    public async Task AiContextController_GetContext_ReturnsNotFound_WhenGameStateIsMissing()
    {
        var service = new FakeAiMasterContextService(null);
        var controller = new AiMasterContextController(
            service,
            new FakeCurrentUserService(Guid.NewGuid()));

        var result = await controller.GetContext(Guid.NewGuid(), 10, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public void AiMasterContextRepository_Source_ContainsMechanicsContextFields()
    {
        var source = ReadRepositoryFile("backend/backend/Modules/Ai/Infrastructure/AiMasterContextRepository.cs");

        Assert.Contains("recent_rolls AS", source);
        Assert.Contains("recent_checks AS", source);
        Assert.Contains("mechanic_requests_doc AS", source);
        Assert.Contains("'последниеБроски'", source);
        Assert.Contains("'последниеПроверки'", source);
        Assert.Contains("'запросыМеханик'", source);
        Assert.Contains("game.dice_rolls", source);
        Assert.Contains("game.skill_checks", source);
        Assert.Contains("game.mechanic_requests", source);
    }

    [Fact]
    public void GameStateDocumentsView_UsesCharactersArray_NotSinglePlayerLimitOne()
    {
        var sql = ReadRepositoryFile("database/init/001_create_schema.sql");

        Assert.Contains("'персонажи'", sql);
        Assert.DoesNotContain("'игрок'", sql);
        Assert.DoesNotContain("LEFT JOIN LATERAL", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("p_inner", sql, StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertActionHas<TAttribute>(Type controllerType, string actionName)
        where TAttribute : Attribute
    {
        var method = controllerType.GetMethods().Single(method => method.Name == actionName);
        Assert.NotNull(method.GetCustomAttribute<TAttribute>());
    }

    private static string ReadRepositoryFile(string relativePath)
    {
        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, normalized);
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            current = current.Parent;
        }

        throw new FileNotFoundException($"Could not find repository file: {relativePath}");
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
            return new CurrentUser(_accountId, "api-smoke@example.com", "api-smoke", "user");
        }
    }

    private sealed class FakeAiMasterContextService : IAiMasterContextService
    {
        private readonly JsonElement? _context;

        public FakeAiMasterContextService(JsonElement? context)
        {
            _context = context;
        }

        public Guid LastAccountId { get; private set; }

        public Guid LastGameStateId { get; private set; }

        public int LastRecentEventsLimit { get; private set; }

        public Task<JsonElement?> GetContextAsync(
            Guid accountId,
            Guid gameStateId,
            int recentEventsLimit,
            CancellationToken cancellationToken)
        {
            LastAccountId = accountId;
            LastGameStateId = gameStateId;
            LastRecentEventsLimit = recentEventsLimit;
            return Task.FromResult(_context);
        }
    }
}
