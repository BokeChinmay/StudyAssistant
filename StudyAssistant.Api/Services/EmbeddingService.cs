using System.Text;
using System.Text.Json;

namespace StudyAssistant.Api.Services;

public class EmbeddingService {
    private readonly IHttpClientFactory _httpFactory;
    private const string OllamaUrl = "http://localhost:11434/api/embeddings";
    private const string Model = "nomic-embed-text";

    public EmbeddingService(IHttpClientFactory httpFactory) {
        _httpFactory = httpFactory;
    }

    public async Task<float[]> GetEmbeddingAsync(string text) {
        var http = _httpFactory.CreateClient();
        var body = JsonSerializer.Serialize(new { model = Model, prompt = text });

        using var request = new HttpRequestMessage(HttpMethod.Post, OllamaUrl) {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

        using var response = await http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var embeddingArray = doc.RootElement.GetProperty("embedding").EnumerateArray().Select(e => e.GetSingle()).ToArray();

        return embeddingArray;
    }

    public async Task<List<float[]>> GetEmbeddingsBatchAsync(List<string> texts) {
        var tasks = texts.Select(GetEmbeddingAsync);
        var results = await Task.WhenAll(tasks);
        return results.ToList();
    }
}