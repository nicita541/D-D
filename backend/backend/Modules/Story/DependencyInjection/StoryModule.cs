using backend.Modules.Story.Application;
using backend.Modules.Story.Infrastructure;

namespace backend.Modules.Story.DependencyInjection;

public static class StoryModule
{
    public static IServiceCollection AddStoryModule(this IServiceCollection services)
    {
        services.AddScoped<IStoryRepository, StoryRepository>();
        services.AddScoped<IStoryService, StoryService>();

        return services;
    }
}
