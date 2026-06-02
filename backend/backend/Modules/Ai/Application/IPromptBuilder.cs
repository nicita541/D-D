using System.Text.Json;

namespace backend.Modules.Ai.Application;

public interface IPromptBuilder
{
    string BuildTurnPrompt(JsonElement context, string playerMessage);

    string BuildRepairPrompt(string invalidResponse, string validationError);
}
