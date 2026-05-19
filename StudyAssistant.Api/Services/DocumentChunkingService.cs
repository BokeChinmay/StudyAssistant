using StudyAssistant.Api.Models;
using System.Text;
using UglyToad.PdfPig;

namespace StudyAssistant.Api.Services;

public class DocumentChunkingService {
    private const int ChunkSize = 400; //Words per chunk
    private const int ChunkOverlap = 80; //Words to overlap between chunks for better context retention

    public List<DocumentChunk> ChunkText(string documentId, string fileName, string text) {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var chunks = new List<DocumentChunk>();
        var index = 0;
        var chunkIndex = 0;

        while (index < words.Length) {
            var end = Math.Min(index + ChunkSize, words.Length);
            var chunkWords = words[index..end];
            var chunkText = string.Join(' ', chunkWords).Trim();

            if(!string.IsNullOrWhiteSpace(chunkText)) {
                chunks.Add(new DocumentChunk {
                    DocumentId = documentId,
                    FileName = fileName,
                    Text = chunkText,
                    ChunkIndex = chunkIndex++
                });
            }

            //Slide forward by ChunkSize - Overlap to create overlapping chunks
            index += ChunkSize - ChunkOverlap;
        }

        return chunks;
    }

    public string ExtractTextFromPdf(Stream pdfStream) {
        var sb = new StringBuilder();
        using var pdf = PdfDocument.Open(pdfStream);

        foreach (var page in pdf.GetPages()) {
            sb.AppendLine(page.Text);
        }

        return sb.ToString();
    }

    public string ExtractTextFromTxt (Stream txtStream) {
        using var reader = new StreamReader(txtStream);
        return reader.ReadToEnd();
    }
}