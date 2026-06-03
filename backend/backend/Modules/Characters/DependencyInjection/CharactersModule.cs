using backend.Modules.Characters.Application;
using backend.Modules.Characters.Infrastructure;

namespace backend.Modules.Characters.DependencyInjection;

public static class CharactersModule
{
    public static IServiceCollection AddCharactersModule(this IServiceCollection services)
    {
        services.AddScoped<ICharacterRepository, CharacterRepository>();
        services.AddScoped<ICharacterDomainRepository, CharacterDomainRepository>();
        services.AddScoped<ICharacterProgressionRepository, CharacterProgressionRepository>();
        services.AddScoped<ICharacterService, CharacterService>();
        services.AddScoped<ICharacterDomainService, CharacterDomainService>();
        services.AddScoped<ICharacterProgressionService, CharacterProgressionService>();

        return services;
    }
}
