using backend.Modules.Auth.Application;
using backend.Modules.Auth.Infrastructure;

namespace backend.Modules.Auth.DependencyInjection;

public static class AuthModule
{
    public static IServiceCollection AddAuthModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        return services;
    }
}
