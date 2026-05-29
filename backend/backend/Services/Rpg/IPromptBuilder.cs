using System.Text.Json;

namespace backend.Services.Rpg;

public interface IPromptBuilder
{
    string BuildTurnPrompt(JsonElement context, string playerMessage);

    string BuildRepairPrompt(string invalidResponse, string validationError);
}
