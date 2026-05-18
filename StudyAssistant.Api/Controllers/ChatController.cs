using Microsoft.AspNetCore.Mvc;
using StudyAssistant.Api.Services;
using StudyAssistant.Api.Models;
using System.Text;
using System.Text.Json;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;
    private readonly ConversationService _conversation;

    public ChatController(IHttpClientFactory httpFactory, IConfiguration config, ConversationService conversation)
    {
        _httpFactory = httpFactory;
        _config = config;
        _conversation = conversation;
    }

    [HttpPost("stream")]
    public async Task StreamMessage([FromBody] ChatRequest request)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";

        var mode = _conversation.DetectMode(request.Message);
        var session = _conversation.GetOrCreate(request.SessionID);
        session.ActiveMode = mode;

        _conversation.AddMessage(request.SessionID, "user", request.Message);

        var messages = session.Messages.Select(m => new { role = m.Role, content = m.Content }).ToList<object>();

        var body = JsonSerializer.Serialize(new
        {
            model = "llama-3.3-70b-versatile",
            max_tokens = 1024,
            stream = true,
            messages = new List<object>
            {
                new { role = "system", content = PromptService.BuildSystemPrompt(mode) }
            }.Concat(messages).ToList()
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

        var fullResponse = new StringBuilder();
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
                    var token = content.GetString()!;
                    fullResponse.Append(token);
                    var encoded = token.Replace("\n", "\\n").Replace("\r", "\\r");
                    await Response.WriteAsync($"data: {encoded}\n\n");
                    await Response.Body.FlushAsync();
                }
            }
            catch (JsonException) { continue; }
        }

        _conversation.AddMessage(request.SessionID, "assistant", fullResponse.ToString());
    }
}

// public record ChatRequest(string Message, List<MessageRecord> History);
// public record MessageRecord(string Role, string Content);