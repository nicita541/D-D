namespace backend.Infrastructure.Ai;

public interface IOllamaClient
{
    Task<OllamaGenerateResult> GenerateAsync(string prompt, CancellationToken cancellationToken);
}

public sealed record OllamaGenerateResult(
    string Model,
    string Response,
    string RawJson);

public sealed class OllamaClientException : Exception
{
    public OllamaClientException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
