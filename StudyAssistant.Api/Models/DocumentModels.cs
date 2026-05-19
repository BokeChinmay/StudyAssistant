namespace StudyAssistant.Api.Models;

public class UploadedDocument {
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FileName { get; init; } = "";
    public string SessionId { get; init; } = "";
    public List<DocumentChunk> Chunks { get; init; } = new();
    public DateTime UploadedAt { get; init; } = DateTime.UtcNow;
}

public class DocumentChunk {
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string DocumentId { get; init; } = "";
    public string FileName { get; init; } = "";
    public string Text { get; init; } = "";
    public int ChunkIndex { get; init; }
    public float[] Embedding { get; set; } = Array.Empty<float>();
}

public class RetrievedContext {
    public string Text { get; init; } = "";
    public string FileName { get; init; } = "";
    public float Similarity { get; init; }
}