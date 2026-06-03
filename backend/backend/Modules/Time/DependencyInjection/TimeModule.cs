using backend.Modules.Time.Application;
using backend.Modules.Time.Infrastructure;

namespace backend.Modules.Time.DependencyInjection;

public static class TimeModule
{
    public static IServiceCollection AddTimeModule(this IServiceCollection services)
    {
        services.AddScoped<ITimeRepository, TimeRepository>();
        services.AddScoped<ITimeService, TimeService>();

        return services;
    }
}
