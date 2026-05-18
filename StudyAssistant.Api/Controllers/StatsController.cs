using Microsoft.AspNetCore.Mvc;
using StudyAssistant.Api.Services;

[ApiController]
[Route("api/[controller]")]
public class StatsController : ControllerBase {
    private readonly ConversationService _conversation;

    public StatsController(ConversationService conversation) {
        _conversation = conversation;
    }

    [HttpGet("{sessionId}")]
    public IActionResult GetStats(string sessionId) {
        var session = _conversation.GetOrCreate(sessionId);
        return Ok(new {
            messageCount = session.Messages.Count,
            quizCorrect = session.QuizCorrect,
            quizTotal = session.QuizTotal,
            flashcardSets = session.FlashcardSetsGenerated,
            activeMode = session.ActiveMode.ToString()
        });
    }

    [HttpPost("{sessionId}/quiz-answer")]
    public IActionResult RecordQuizAnswer(string sessionId, [FromBody] QuizAnswerRequest request) {
        var session = _conversation.GetOrCreate(sessionId);
        session.QuizTotal++;
        if(request.Correct) session.QuizCorrect++;
        return Ok();
    }
}

public record QuizAnswerRequest(bool Correct);