using StudyAssistant.Web.Models;
using System.Text.RegularExpressions;

namespace StudyAssistant.Web.Services;

public static class FlashcardParser {
    private static readonly Regex CardPattern = new(@"FLASHCARD\s+\d+\s*Q:\s*(.+?)\s*A:\s*(.+?)(?=FLASHCARD|\z)", RegexOptions.Singleline | RegexOptions.Compiled);

    public static bool IsFlashcardResponse(string text) => text.TrimStart().StartsWith("FLASHCARD");

    public static List<Flashcard> Parse(string text) {
        var cards = new List<Flashcard>();
        var matches = CardPattern.Matches(text);

        foreach (Match match in matches) {
            var question = match.Groups[1].Value.Trim();
            var answer = match.Groups[2].Value.Trim();

            if(!string.IsNullOrEmpty(question) && !string.IsNullOrEmpty(answer)) {
                cards.Add(new Flashcard(question, answer));
            }
        }
        
        return cards;
    }
}