namespace StudyAssistant.Web.Models;

public record Flashcard(string Question, string Answer);

public class FlashcardDeckState {
    public List<Flashcard> Cards { get; init; } = new();
    public int CurrentIndex { get; set; } = 0;
    public bool IsFlipped { get; set; } = false;
    public HashSet<int> MarkedForReview { get; } = new();

    public Flashcard Current => Cards[CurrentIndex];
    public bool HasPrevious => CurrentIndex > 0;
    public bool HasNext => CurrentIndex < Cards.Count - 1;
}