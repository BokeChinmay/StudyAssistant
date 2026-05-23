using System.Text;

namespace StudyAssistant.Api.Services;

public class EmbeddingService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;

    public EmbeddingService(IHttpClientFactory httpFactory, IConfiguration config)
    {
        _httpFactory = httpFactory;
        _config = config;
    }

    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        // Try HuggingFace first, fall back to local if it fails
        try
        {
            return await GetHuggingFaceEmbeddingAsync(text);
        }
        catch
        {
            return GetLocalEmbedding(text);
        }
    }

    public async Task<List<float[]>> GetEmbeddingsBatchAsync(List<string> texts)
    {
        var results = new List<float[]>();
        foreach (var text in texts)
        {
            results.Add(await GetEmbeddingAsync(text));
            await Task.Delay(100);
        }
        return results;
    }

    private async Task<float[]> GetHuggingFaceEmbeddingAsync(string text)
    {
        var http = _httpFactory.CreateClient();
        http.Timeout = TimeSpan.FromSeconds(10);
        var apiKey = _config["HuggingFace:ApiKey"];
        const string ApiUrl = "https://api-inference.huggingface.co/pipeline/feature-extraction/sentence-transformers/all-MiniLM-L6-v2";

        using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
        request.Headers.Add("Authorization", $"Bearer {apiKey}");
        request.Content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(
                new { inputs = text, options = new { wait_for_model = true } }),
            Encoding.UTF8, "application/json");

        using var response = await http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.ValueKind == System.Text.Json.JsonValueKind.Array &&
            root[0].ValueKind == System.Text.Json.JsonValueKind.Array)
            return root[0].EnumerateArray().Select(e => e.GetSingle()).ToArray();

        return root.EnumerateArray().Select(e => e.GetSingle()).ToArray();
    }

    private static float[] GetLocalEmbedding(string text)
    {
        // Simple but effective: character n-gram hash embedding
        // Produces a 384-dim vector (same as all-MiniLM-L6-v2)
        const int Dims = 384;
        var vector = new float[Dims];
        var words = text.ToLower()
            .Split(new[] { ' ', '\n', '\t', '.', ',', '!', '?' }, 
                StringSplitOptions.RemoveEmptyEntries);

        foreach (var word in words)
        {
            // Hash the word into multiple dimensions
            var hash1 = (uint)word.GetHashCode();
            var hash2 = (uint)(word + "_").GetHashCode();
            var hash3 = (uint)("_" + word).GetHashCode();

            for (int i = 0; i < word.Length; i++)
            {
                var idx1 = (int)((hash1 + word[i] * 31 + i * 7) % Dims);
                var idx2 = (int)((hash2 + word[i] * 17 + i * 13) % Dims);
                var idx3 = (int)((hash3 + word[i] * 37 + i * 3) % Dims);

                vector[idx1] += 1.0f;
                vector[idx2] += 0.5f;
                vector[idx3] += 0.25f;
            }
        }

        // L2 normalize
        var magnitude = MathF.Sqrt(vector.Sum(v => v * v));
        if (magnitude > 0)
            for (int i = 0; i < Dims; i++)
                vector[i] /= magnitude;

        return vector;
    }
}