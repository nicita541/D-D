using backend.Modules.Ai.Application;
using backend.Modules.Ai.Infrastructure;

namespace backend.Modules.Ai.DependencyInjection;

public static class AiModule
{
    public static IServiceCollection AddAiModule(this IServiceCollection services)
    {
        services.AddScoped<IAiMasterContextRepository, AiMasterContextRepository>();
        services.AddScoped<IAiMasterContextService, AiMasterContextService>();
        services.AddScoped<IPromptBuilder, PromptBuilder>();

        return services;
    }
}
