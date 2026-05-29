namespace backend.Infrastructure.Ai;

public sealed class AiMasterOptions
{
    public const string SectionName = "Ollama";

    public string BaseUrl { get; set; } = "http://llm:11434";

    public string Model { get; set; } = "llama3.1";

    public int TimeoutSeconds { get; set; } = 120;
}
