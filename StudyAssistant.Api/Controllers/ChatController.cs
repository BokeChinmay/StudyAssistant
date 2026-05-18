using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;

    public ChatController(IHttpClientFactory httpFactory, IConfiguration config)
    {
        _httpFactory = httpFactory;
        _config = config;
    }

    [HttpPost("stream")]
    public async Task StreamMessage([FromBody] ChatRequest request)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";

        var messages = new List<object>
        {
            new { role = "system", content = "You are a focused study assistant. Help the user understand concepts clearly, generate flashcards on request, and quiz them on material they provide." }
        };

        foreach (var m in request.History)
            messages.Add(new { role = m.Role, content = m.Content });

        messages.Add(new { role = "user", content = request.Message });

        var body = JsonSerializer.Serialize(new
        {
            model = "llama-3.3-70b-versatile",
            max_tokens = 1024,
            stream = true,
            messages
        });

        var http = _httpFactory.CreateClient();
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");
        httpRequest.Headers.Add("Authorization", $"Bearer {_config["Groq:ApiKey"]}");
        httpRequest.Content = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await http.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            await Response.WriteAsync($"data: [ERROR] {error}\n\n");
            return;
        }

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (line?.StartsWith("data: ") != true) continue;

            var json = line[6..];
            if (json == "[DONE]") break;

            try
            {
                using var doc = JsonDocument.Parse(json);
                var delta = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("delta");

                if (delta.TryGetProperty("content", out var content) &&
                    content.ValueKind == JsonValueKind.String)
                {
                    await Response.WriteAsync($"data: {content.GetString()}\n\n");
                    await Response.Body.FlushAsync();
                }
            }
            catch (JsonException) { continue; }
        }
    }
}

public record ChatRequest(string Message, List<MessageRecord> History);
public record MessageRecord(string Role, string Content);