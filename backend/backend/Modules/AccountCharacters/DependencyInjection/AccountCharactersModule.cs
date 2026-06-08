using backend.Modules.AccountCharacters.Application;
using backend.Modules.AccountCharacters.Infrastructure;

namespace backend.Modules.AccountCharacters.DependencyInjection;

public static class AccountCharactersModule
{
    public static IServiceCollection AddAccountCharactersModule(this IServiceCollection services)
    {
        services.AddScoped<IAccountCharacterRepository, AccountCharacterRepository>();
        services.AddScoped<IAccountCharacterSyncRepository, AccountCharacterSyncRepository>();
        services.AddScoped<IAccountCharacterSyncService, AccountCharacterSyncService>();
        services.AddScoped<IAccountCharacterService, AccountCharacterService>();

        return services;
    }
}
