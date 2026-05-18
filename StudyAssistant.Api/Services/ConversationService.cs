using Microsoft.Extensions.Caching.Memory;
using StudyAssistant.Api.Models;

namespace StudyAssistant.Api.Services;

public class ConversationService{
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan SessionTimeout = TimeSpan.FromHours(2);

    public ConversationService(IMemoryCache cache){
        _cache = cache;
    }

    public ConversationSession GetOrCreate(string sessionId) {
        return _cache.GetOrCreate(sessionId, entry => {
            entry.SlidingExpiration = SessionTimeout;
            return new ConversationSession { LastActivity = DateTime.UtcNow }; 
        })!;
    }

    public void AddMessage(string sessionId, string role, string content) {
        var session = GetOrCreate(sessionId);
        session.Messages.Add(new ChatMessage(role, content));
        session.LastActivity = DateTime.UtcNow;

        if(session.Messages.Count > 20) {
            session.Messages.RemoveAt(0);
        };
    }

    public StudyMode DetectMode(string message) {
        var lower = message.ToLower();

        if(lower.Contains("flashcard") || lower.Contains("flash card") || lower.Contains("make cards") || lower.Contains("create cards"))
            return StudyMode.Flashcards;

        if(lower.Contains("quiz me") || lower.Contains("test me") || lower.Contains("ask me") || lower.Contains("question me"))
            return StudyMode.Quiz;

        return StudyMode.General;
    }
}