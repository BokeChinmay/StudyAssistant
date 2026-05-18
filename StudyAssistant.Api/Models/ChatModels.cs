namespace StudyAssistant.Api.Models;

public record ChatMessage(string Role, string Content);

public class ConversationSession{
    public string SessionID { get; init; } = Guid.NewGuid().ToString();
    public List<ChatMessage> Messages { get; } = new();
    public StudyMode ActiveMode { get; set; } = StudyMode.General;
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;
}

public enum StudyMode{
    General,
    Flashcards,
    Quiz
}

public record ChatRequest(string Message, string SessionID);

public record MessageRecord(string Role, string Content);