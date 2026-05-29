using System.Text;
using System.Text.Json;

namespace backend.Services.Rpg;

public sealed class PromptBuilder : IPromptBuilder
{
    public string BuildTurnPrompt(JsonElement context, string playerMessage)
    {
        var builder = new StringBuilder();
        builder.AppendLine("You are an RPG game master. Continue the scene using the provided state.");
        builder.AppendLine("Do not directly mutate game state. Return either plain text, or a JSON object:");
        builder.AppendLine("""{"master_answer":"text","changes":[{"operation":"добавить_запись_журнала","payload":{}}]}""");
        builder.AppendLine("Only propose changes in the changes array. Keep payloads structured JSON.");
        builder.AppendLine();
        builder.AppendLine("GAME STATE JSON:");
        builder.AppendLine(context.GetRawText());
        builder.AppendLine();
        builder.AppendLine("PLAYER MESSAGE:");
        builder.AppendLine(playerMessage);
        return builder.ToString();
    }
}
