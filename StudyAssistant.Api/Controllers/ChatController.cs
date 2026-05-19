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
    private readonly DocumentService _documentService;

    public ChatController(IHttpClientFactory httpFactory, IConfiguration config, ConversationService conversation, DocumentService documentService)
    {
        _httpFactory = httpFactory;
        _config = config;
        _conversation = conversation;
        _documentService = documentService;
    }

    [HttpPost("stream")]
    public async Task StreamMessage([FromBody] ChatRequest request)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";

        var session = _conversation.GetOrCreate(request.SessionID);
        var detected = _conversation.DetectMode(request.Message);

        StudyMode mode;

        if(detected == StudyMode.Flashcards || detected == StudyMode.Quiz) {
            mode = detected;

            if(detected == StudyMode.Quiz) session.QuizActive = false;
        } 
        else if (_conversation.IsExitRequest(request.Message)) {
            mode = StudyMode.General;
            session.QuizActive = false;
        }
        else if(session.ActiveMode == StudyMode.Quiz && !_conversation.IsGeneralQuestion(request.Message)) {
            mode = StudyMode.Quiz;
        }
        else {
            mode = StudyMode.General;
        }

        session.ActiveMode = mode;

        //RAG: retrieve relevant ccontext if documents exist for this session
        var ragContext = "";
        var docNames = _documentService.GetDocumentNames(request.SessionID);

        if(docNames.Count > 0) {
            var retrieved = await _documentService.QueryAsync(request.SessionID, request.Message);
            
            if (retrieved.Count > 0) {
                var contextBuilder = new StringBuilder();
                contextBuilder.AppendLine("Relevant context from the user's uploaded documents:");
                contextBuilder.AppendLine();

                foreach (var ctx in retrieved) {
                    contextBuilder.AppendLine($"[From {ctx.FileName}]");
                    contextBuilder.AppendLine(ctx.Text);
                    contextBuilder.AppendLine();
                }

                ragContext = contextBuilder.ToString();
            }
        }

        _conversation.AddMessage(request.SessionID, "user", request.Message);

        var messages = session.Messages.Select(m => new { role = m.Role, content = m.Content }).ToList<object>();

        var systemPrompt = PromptService.BuildSystemPrompt(mode);

        if(!string.IsNullOrEmpty(ragContext)) {
            systemPrompt += $"\n\n{ragContext}\n\nAnswer using the context above where relevant. If the answer isn't in the context, say so and answer from general knowledge.";
        }

        var body = JsonSerializer.Serialize(new
        {
            model = "llama-3.3-70b-versatile",
            max_tokens = 1024,
            stream = true,
            messages = new List<object>
            {
                new { role = "system", content = systemPrompt }
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

        if(mode == StudyMode.Quiz) {
            if(!session.QuizActive) {
                session.QuizActive = true;
            }
            else {
                var lower = fullResponse.ToString().ToLower();
                var isCorrect = lower.Contains("that's right") ||
                                lower.Contains("thats right") ||
                                lower.Contains("well done") ||
                                lower.Contains("exactly right") ||
                                lower.Contains("that is correct") ||
                                lower.Contains("you're correct") ||
                                lower.Contains("youre correct") ||
                                lower.Contains("yes, correct") ||
                                lower.Contains("yes! correct");
                var isIncorrect = lower.Contains("incorrect") ||
                                  lower.Contains("not quite") ||
                                  lower.Contains("that's not right") ||
                                  lower.Contains("thats not right") ||
                                  lower.Contains("not right") ||
                                  lower.Contains("wrong") ||
                                  lower.Contains("that is incorrect") ||
                                  lower.Contains("not correct");

                if(isCorrect || isIncorrect) {
                    session.QuizTotal++;
                    if(isCorrect && !isIncorrect) session.QuizCorrect++;
                }
            }    
        }
 
        if(mode == StudyMode.Flashcards) {
            _conversation.IncrementFlashcardSets(request.SessionID);
            session.ActiveMode = StudyMode.General;
        }

        _conversation.AddMessage(request.SessionID, "assistant", fullResponse.ToString());
    }
}

// public record ChatRequest(string Message, List<MessageRecord> History);
// public record MessageRecord(string Role, string Content);