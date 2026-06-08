using backend.Modules.Snapshots.Application;
using backend.Modules.Snapshots.Infrastructure;

namespace backend.Modules.Snapshots.DependencyInjection;

public static class SnapshotsModule
{
    public static IServiceCollection AddSnapshotsModule(this IServiceCollection services)
    {
        services.AddScoped<ISnapshotRepository, SnapshotRepository>();
        services.AddScoped<ISnapshotService, SnapshotService>();
        return services;
    }
}
