using System.Reflection;
using backend.Controllers;
using backend.Controllers.Rpg;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace Tests.Controllers;

public sealed class AllApiSurfaceContractTests
{
    private static readonly Assembly BackendAssembly = typeof(backend.Program).Assembly;

    [Fact]
    public void All_Api_Controllers_Have_ApiController()
    {
        var failures = GetApiControllers()
            .Where(controller => controller.GetCustomAttribute<ApiControllerAttribute>() is null)
            .Select(controller => $"{controller.Name} is missing [ApiController].")
            .ToArray();

        Assert.Empty(failures);
    }

    [Fact]
    public void All_Controller_Actions_Have_Explicit_Http_Method_Attributes()
    {
        var failures = new List<string>();

        foreach (var controller in GetApiControllers())
        {
            foreach (var method in GetDeclaredPublicControllerMethods(controller))
            {
                if (GetHttpMethodAttributes(method).Length == 0)
                {
                    failures.Add($"{controller.Name}.{method.Name} has no explicit HTTP method attribute.");
                }
            }
        }

        Assert.Empty(failures);
    }

    [Fact]
    public void Health_And_Auth_Public_Surface_Is_Explicit()
    {
        var failures = new List<string>();

        if (typeof(HealthController).GetCustomAttribute<AllowAnonymousAttribute>() is null)
        {
            failures.Add("HealthController must be [AllowAnonymous].");
        }

        AssertActionHas<AllowAnonymousAttribute>(typeof(AuthController), nameof(AuthController.Register), failures);
        AssertActionHas<AllowAnonymousAttribute>(typeof(AuthController), nameof(AuthController.Login), failures);
        AssertActionHas<AllowAnonymousAttribute>(typeof(AuthController), nameof(AuthController.Refresh), failures);
        AssertActionHas<AuthorizeAttribute>(typeof(AuthController), nameof(AuthController.Me), failures);
        AssertActionHas<AuthorizeAttribute>(typeof(AuthController), nameof(AuthController.Logout), failures);

        Assert.Empty(failures);
    }

    [Fact]
    public void Protected_Rpg_Controllers_Are_Authorized()
    {
        var failures = new List<string>();

        foreach (var controller in GetApiControllers())
        {
            if (controller == typeof(HealthController) ||
                controller == typeof(AuthController) ||
                controller.Name == "CampaignsController")
            {
                continue;
            }

            if (controller.GetCustomAttribute<AuthorizeAttribute>() is null)
            {
                failures.Add($"{controller.Name} must be protected by [Authorize].");
            }
        }

        Assert.Empty(failures);
    }

    [Fact]
    public void No_Duplicate_Http_Method_And_Route_Combinations()
    {
        var seen = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var failures = new List<string>();

        foreach (var endpoint in GetEndpointContracts())
        {
            foreach (var method in endpoint.HttpMethods)
            {
                var key = $"{method} {endpoint.FullRoute}";
                var owner = $"{endpoint.Controller.Name}.{endpoint.Action.Name}";

                if (seen.TryGetValue(key, out var existing))
                {
                    failures.Add($"Duplicate endpoint {key}: {existing} and {owner}.");
                }
                else
                {
                    seen[key] = owner;
                }
            }
        }

        Assert.Empty(failures);
    }

    [Fact]
    public void Route_Parameters_Match_Action_Parameters()
    {
        var failures = new List<string>();

        foreach (var endpoint in GetEndpointContracts())
        {
            var routeParameters = ExtractRouteParameterNames(endpoint.FullRoute)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var actionParameters = endpoint.Action.GetParameters()
                .Select(parameter => parameter.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var routeParameter in routeParameters)
            {
                if (!actionParameters.Contains(routeParameter))
                {
                    failures.Add($"{endpoint.Controller.Name}.{endpoint.Action.Name} route parameter {{{routeParameter}}} has no matching action parameter. Route: {endpoint.FullRoute}");
                }
            }
        }

        Assert.Empty(failures);
    }

    [Fact]
    public void Async_Controller_Actions_Accept_CancellationToken()
    {
        var failures = new List<string>();

        foreach (var controller in GetApiControllers())
        {
            foreach (var action in GetControllerActions(controller))
            {
                if (!typeof(Task).IsAssignableFrom(action.ReturnType))
                {
                    continue;
                }

                var hasCancellationToken = action.GetParameters()
                    .Any(parameter => parameter.ParameterType == typeof(CancellationToken));

                if (!hasCancellationToken)
                {
                    failures.Add($"{controller.Name}.{action.Name} is async but has no CancellationToken parameter.");
                }
            }
        }

        Assert.Empty(failures);
    }

    [Fact]
    public void All_Endpoints_Are_Under_Api_Or_Health()
    {
        var failures = new List<string>();

        foreach (var endpoint in GetEndpointContracts())
        {
            if (!endpoint.FullRoute.StartsWith("api/", StringComparison.OrdinalIgnoreCase) &&
                !endpoint.FullRoute.Equals("health", StringComparison.OrdinalIgnoreCase) &&
                !endpoint.FullRoute.Equals("health/db", StringComparison.OrdinalIgnoreCase))
            {
                failures.Add($"{endpoint.Controller.Name}.{endpoint.Action.Name} has unexpected route: {endpoint.FullRoute}");
            }
        }

        Assert.Empty(failures);
    }

    [Fact]
    public void Api_Surface_Has_Core_Rpg_Endpoints()
    {
        var endpoints = GetEndpointContracts()
            .SelectMany(endpoint => endpoint.HttpMethods.Select(method => $"{method} {endpoint.FullRoute}"))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var required = new[]
        {
            "GET health",
            "GET health/db",

            "POST api/auth/register",
            "POST api/auth/login",
            "POST api/auth/refresh",
            "POST api/auth/logout",
            "GET api/auth/me",

            "GET api/game-states",
            "POST api/game-states",
            "GET api/game-states/{gameStateId:guid}",
            "DELETE api/game-states/{gameStateId:guid}",

            "GET api/game-states/{gameStateId:guid}/characters",
            "POST api/game-states/{gameStateId:guid}/characters",
            "GET api/game-states/{gameStateId:guid}/characters/{characterId:guid}",
            "PUT api/game-states/{gameStateId:guid}/characters/{characterId:guid}",
            "DELETE api/game-states/{gameStateId:guid}/characters/{characterId:guid}",

            "GET api/game-states/{gameStateId:guid}/ai-context",

            "POST api/game-states/{gameStateId:guid}/rolls",
            "POST api/game-states/{gameStateId:guid}/checks/ability",
            "GET api/game-states/{gameStateId:guid}/checks",

            "GET api/game-states/{gameStateId:guid}/memory",
            "PUT api/game-states/{gameStateId:guid}/memory",
            "POST api/game-states/{gameStateId:guid}/memory/summarize",

            "GET api/game-states/{gameStateId:guid}/mechanic-requests",
            "POST api/game-states/{gameStateId:guid}/mechanic-requests/{requestId:guid}/resolve/ability-check",

            "GET api/game-states/{gameStateId:guid}/changes",
            "POST api/game-states/{gameStateId:guid}/changes/{changeId:guid}/apply",
            "POST api/game-states/{gameStateId:guid}/changes/{changeId:guid}/reject",

            "GET api/game-states/{gameStateId:guid}/turns",
            "POST api/game-states/{gameStateId:guid}/turns",
            "GET api/game-states/{gameStateId:guid}/turns/{turnId:guid}",

            "GET api/game-states/{gameStateId:guid}/world",
            "POST api/game-states/{gameStateId:guid}/world/locations",
            "POST api/game-states/{gameStateId:guid}/world/objects",
            "POST api/game-states/{gameStateId:guid}/world/containers",
            "POST api/game-states/{gameStateId:guid}/world/npcs",
            "POST api/game-states/{gameStateId:guid}/world/quests",
            "POST api/game-states/{gameStateId:guid}/world/monsters",

            "GET api/game-states/{gameStateId:guid}/combat",
            "POST api/game-states/{gameStateId:guid}/combat/start",
            "POST api/game-states/{gameStateId:guid}/combat/participants",
            "POST api/game-states/{gameStateId:guid}/combat/next-turn",
            "POST api/game-states/{gameStateId:guid}/combat/attack",
            "POST api/game-states/{gameStateId:guid}/combat/apply-damage",
            "POST api/game-states/{gameStateId:guid}/combat/participants/{participantId:guid}/heal",
            "POST api/game-states/{gameStateId:guid}/combat/end",

            "POST api/game-states/{gameStateId:guid}/characters/{characterId:guid}/experience",
            "POST api/game-states/{gameStateId:guid}/characters/{characterId:guid}/level-up"
        };

        var missing = required
            .Where(endpoint => !endpoints.Contains(endpoint))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.Empty(missing);
    }

    [Fact]
    public void Ai_Context_And_Game_State_Document_Source_Are_Updated_For_New_Architecture()
    {
        var aiContextRepository = ReadRepositoryFile("backend/backend/Repositories/Rpg/AiMasterContextRepository.cs");

        Assert.Contains("recent_rolls AS", aiContextRepository);
        Assert.Contains("recent_checks AS", aiContextRepository);
        Assert.Contains("mechanic_requests_doc AS", aiContextRepository);
        Assert.Contains("'последниеБроски'", aiContextRepository);
        Assert.Contains("'последниеПроверки'", aiContextRepository);
        Assert.Contains("'запросыМеханик'", aiContextRepository);
        Assert.Contains("game.dice_rolls", aiContextRepository);
        Assert.Contains("game.skill_checks", aiContextRepository);
        Assert.Contains("game.mechanic_requests", aiContextRepository);

        var schema = ReadRepositoryFile("database/init/001_create_schema.sql");
        var gameStateDocumentsView = ExtractGameStateDocumentsView(schema);

        Assert.Contains("CREATE OR REPLACE VIEW game.game_state_documents AS", gameStateDocumentsView);
        Assert.Contains("'персонажи'", gameStateDocumentsView);
        Assert.Contains("jsonb_agg", gameStateDocumentsView);

        Assert.DoesNotContain("'игрок'", gameStateDocumentsView);
        Assert.DoesNotContain("LEFT JOIN LATERAL", gameStateDocumentsView.ToUpperInvariant());
        Assert.DoesNotContain("p_inner", gameStateDocumentsView);
        Assert.DoesNotContain("LIMIT 1", gameStateDocumentsView.ToUpperInvariant());
    }

    private static IReadOnlyList<Type> GetApiControllers()
    {
        return BackendAssembly
            .GetTypes()
            .Where(type =>
                !type.IsAbstract &&
                typeof(ControllerBase).IsAssignableFrom(type) &&
                type.Name.EndsWith("Controller", StringComparison.Ordinal))
            .OrderBy(type => type.Name, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<MethodInfo> GetDeclaredPublicControllerMethods(Type controller)
    {
        return controller
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method =>
                !method.IsSpecialName &&
                method.GetCustomAttribute<NonActionAttribute>() is null)
            .OrderBy(method => method.Name, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<MethodInfo> GetControllerActions(Type controller)
    {
        return GetDeclaredPublicControllerMethods(controller)
            .Where(method => GetHttpMethodAttributes(method).Length > 0)
            .ToArray();
    }

    private static IActionHttpMethodProvider[] GetHttpMethodAttributes(MethodInfo method)
    {
        return method.GetCustomAttributes(inherit: true)
            .OfType<IActionHttpMethodProvider>()
            .ToArray();
    }

    private static IReadOnlyList<EndpointContract> GetEndpointContracts()
    {
        var endpoints = new List<EndpointContract>();

        foreach (var controller in GetApiControllers())
        {
            var controllerRoute = controller.GetCustomAttribute<RouteAttribute>()?.Template ?? string.Empty;

            foreach (var action in GetControllerActions(controller))
            {
                foreach (var httpAttribute in GetHttpMethodAttributes(action))
                {
                    var actionTemplate = GetRouteTemplate(httpAttribute);
                    var route = CombineRoutes(controllerRoute, actionTemplate);

                    endpoints.Add(new EndpointContract(
                        controller,
                        action,
                        route,
                        httpAttribute.HttpMethods.ToArray()));
                }
            }
        }

        return endpoints
            .OrderBy(endpoint => endpoint.FullRoute, StringComparer.OrdinalIgnoreCase)
            .ThenBy(endpoint => endpoint.Controller.Name, StringComparer.Ordinal)
            .ThenBy(endpoint => endpoint.Action.Name, StringComparer.Ordinal)
            .ToArray();
    }

    private static string GetRouteTemplate(IActionHttpMethodProvider httpAttribute)
    {
        return httpAttribute is IRouteTemplateProvider routeTemplateProvider
            ? routeTemplateProvider.Template ?? string.Empty
            : string.Empty;
    }

    private static string CombineRoutes(string controllerRoute, string actionRoute)
    {
        if (actionRoute.StartsWith("/", StringComparison.Ordinal) ||
            actionRoute.StartsWith("~/", StringComparison.Ordinal))
        {
            return NormalizeRoute(actionRoute);
        }

        if (string.IsNullOrWhiteSpace(controllerRoute))
        {
            return NormalizeRoute(actionRoute);
        }

        if (string.IsNullOrWhiteSpace(actionRoute))
        {
            return NormalizeRoute(controllerRoute);
        }

        return NormalizeRoute($"{controllerRoute}/{actionRoute}");
    }

    private static string NormalizeRoute(string route)
    {
        return route
            .Replace("~/", string.Empty, StringComparison.Ordinal)
            .Trim('/')
            .Replace("//", "/", StringComparison.Ordinal);
    }

    private static IEnumerable<string> ExtractRouteParameterNames(string route)
    {
        foreach (var part in route.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!part.StartsWith("{", StringComparison.Ordinal) ||
                !part.EndsWith("}", StringComparison.Ordinal))
            {
                continue;
            }

            var name = part.Trim('{', '}');

            var colonIndex = name.IndexOf(':', StringComparison.Ordinal);
            if (colonIndex >= 0)
            {
                name = name[..colonIndex];
            }

            var equalsIndex = name.IndexOf('=', StringComparison.Ordinal);
            if (equalsIndex >= 0)
            {
                name = name[..equalsIndex];
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                yield return name;
            }
        }
    }

    private static void AssertActionHas<TAttribute>(Type controllerType, string actionName, List<string> failures)
        where TAttribute : Attribute
    {
        var method = controllerType.GetMethods().Single(method => method.Name == actionName);
        if (method.GetCustomAttribute<TAttribute>() is null)
        {
            failures.Add($"{controllerType.Name}.{actionName} is missing [{typeof(TAttribute).Name}].");
        }
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

    private static string ExtractGameStateDocumentsView(string schema)
    {
        const string startMarker = "CREATE OR REPLACE VIEW game.game_state_documents AS";
        const string endMarker = "FROM game.game_states gs;";

        var start = schema.IndexOf(startMarker, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
        {
            throw new InvalidOperationException("Could not find game.game_state_documents view start.");
        }

        var end = schema.IndexOf(endMarker, start, StringComparison.OrdinalIgnoreCase);
        if (end < 0)
        {
            throw new InvalidOperationException("Could not find game.game_state_documents view end.");
        }

        return schema[start..(end + endMarker.Length)];
    }

    private sealed record EndpointContract(
        Type Controller,
        MethodInfo Action,
        string FullRoute,
        IReadOnlyList<string> HttpMethods);
}
