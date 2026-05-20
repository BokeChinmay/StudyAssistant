using System.Text;
using System.Text.Json;

namespace StudyAssistant.Api.Services;

public class EmbeddingService {
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;
    private const string Model = "sentence-transformers/all-MiniLM-L6-v2";
    private const string ApiUrl = "https://api-inference.huggingface.co/pipeline/feature-extraction";
    // private const string OllamaUrl = "http://localhost:11434/api/embeddings";
    // private const string Model = "nomic-embed-text";

    public EmbeddingService(IHttpClientFactory httpFactory, IConfiguration config) {
        _httpFactory = httpFactory;
        _config = config;
    }

    public async Task<float[]> GetEmbeddingAsync(string text) {
        var http = _httpFactory.CreateClient();
        var apiKey = _config["HuggingFace:ApiKey"];

        using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl + "/" + Model);

        request.Headers.Add("Authorization", $"Bearer {apiKey}");
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { inputs =text, options = new { wait_for_model = true } }),
            Encoding.UTF8, 
            "application/json");

        using var response = await http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var root = doc.RootElement;

        if(root.ValueKind == JsonValueKind.Array &&
           root[0].ValueKind == JsonValueKind.Array) { 
            return root[0].EnumerateArray().Select(e => e.GetSingle()).ToArray();
        }

        return root.EnumerateArray().Select(e => e.GetSingle()).ToArray();
    }

    public async Task<List<float[]>> GetEmbeddingsBatchAsync(List<string> texts) {
        var results = new List<float[]>();
        foreach(var text in texts) {
            results.Add(await GetEmbeddingAsync(text));
            await Task.Delay(100); // To avoid hitting rate limits
        }
        return results;
    }
}