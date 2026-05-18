namespace StudyAssistant.Api.Models;

public record ChatMessage(string Role, string Content);

public class ConversationSession{
    public string SessionID { get; init; } = Guid.NewGuid().ToString();
    public List<ChatMessage> Messages { get; } = new();
    public StudyMode ActiveMode { get; set; } = StudyMode.General;
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    public int QuizCorrect { get; set; } = 0;
    public int QuizTotal { get; set; } = 0;
    public bool QuizActive { get; set; } = false;
    public int FlashcardSetsGenerated { get; set; } = 0;
}

public enum StudyMode{
    General,
    Flashcards,
    Quiz
}

public record ChatRequest(string Message, string SessionID);

public record MessageRecord(string Role, string Content);