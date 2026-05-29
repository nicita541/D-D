using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace backend.Infrastructure.Ai;

public sealed class OllamaClient : IOllamaClient
{
    private readonly HttpClient _httpClient;
    private readonly AiMasterOptions _options;

    public OllamaClient(HttpClient httpClient, IOptions<AiMasterOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;

        if (Uri.TryCreate(_options.BaseUrl, UriKind.Absolute, out var baseUri))
        {
            _httpClient.BaseAddress = baseUri;
        }

        _httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(1, _options.TimeoutSeconds));
    }

    public async Task<OllamaGenerateResult> GenerateAsync(string prompt, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "/api/generate",
                new
                {
                    model = _options.Model,
                    prompt,
                    stream = false
                },
                cancellationToken);

            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new OllamaClientException($"Ollama returned {(int)response.StatusCode}: {raw}");
            }

            using var document = JsonDocument.Parse(raw);
            var content = document.RootElement.TryGetProperty("response", out var responseElement)
                ? responseElement.GetString() ?? string.Empty
                : raw;

            var model = document.RootElement.TryGetProperty("model", out var modelElement)
                ? modelElement.GetString() ?? _options.Model
                : _options.Model;

            return new OllamaGenerateResult(model, content, raw);
        }
        catch (OllamaClientException)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            throw new OllamaClientException("Ollama request failed.", ex);
        }
    }
}
