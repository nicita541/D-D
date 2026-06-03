using backend.Infrastructure.DependencyInjection;
using backend.Modules.Ai.DependencyInjection;
using backend.Modules.Auth.DependencyInjection;
using backend.Modules.Campaigns.DependencyInjection;
using backend.Modules.Characters.DependencyInjection;
using backend.Modules.Changes.DependencyInjection;
using backend.Modules.Combat.DependencyInjection;
using backend.Modules.Conditions.DependencyInjection;
using backend.Modules.Economy.DependencyInjection;
using backend.Modules.GameStates.DependencyInjection;
using backend.Modules.Mechanics.DependencyInjection;
using backend.Modules.Memory.DependencyInjection;
using backend.Modules.Party.DependencyInjection;
using backend.Modules.Play.DependencyInjection;
using backend.Modules.Rest.DependencyInjection;
using backend.Modules.Story.DependencyInjection;
using backend.Modules.Time.DependencyInjection;
using backend.Modules.Travel.DependencyInjection;
using backend.Modules.Turns.DependencyInjection;
using backend.Modules.World.DependencyInjection;

namespace backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddInfrastructure(builder.Configuration);

            builder.Services.AddAuthModule(builder.Configuration);
            builder.Services.AddGameStatesModule();
            builder.Services.AddCharactersModule();
            builder.Services.AddWorldModule();
            builder.Services.AddTravelModule();
            builder.Services.AddCombatModule();
            builder.Services.AddTimeModule();
            builder.Services.AddRestModule();
            builder.Services.AddConditionsModule();
            builder.Services.AddEconomyModule();
            builder.Services.AddChangesModule();
            builder.Services.AddTurnsModule();
            builder.Services.AddMechanicsModule();
            builder.Services.AddMemoryModule();
            builder.Services.AddAiModule();
            builder.Services.AddCampaignsModule();
            builder.Services.AddPartyModule();
            builder.Services.AddStoryModule();
            builder.Services.AddPlayModule();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
