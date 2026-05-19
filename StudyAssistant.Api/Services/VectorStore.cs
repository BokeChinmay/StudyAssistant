using StudyAssistant.Api.Models;

namespace StudyAssistant.Api.Services;

public class VectorStore {
    // SessionID + List of chunks with embeddings
    private readonly Dictionary<string, List<DocumentChunk>> _store = new();
    private readonly Lock _lock = new();

    public void AddChunks(string sessionId, List<DocumentChunk> chunks) {
        lock(_lock) {
            if(!_store.ContainsKey(sessionId)) {
                _store[sessionId] = new List<DocumentChunk>();
            }
            _store[sessionId].AddRange(chunks);
        }
    }

    public List<RetrievedContext> Search (string sessionId, float[] queryEmbedding, int topK = 4) {
        lock (_lock) {
            if(!_store.TryGetValue(sessionId, out var chunks) || chunks.Count == 0) {
                return new List<RetrievedContext>();
            }

            return chunks
                    .Where(c => c.Embedding.Length > 0)
                    .Select(c => new RetrievedContext {
                            Text = c.Text,
                            FileName = c.FileName,
                            Similarity = CosineSimilarity(queryEmbedding, c.Embedding)})
                    .OrderByDescending(r => r.Similarity)
                    .Take(topK)
                    .Where(r => r.Similarity > 0.4f)
                    .ToList();
        }
    }

    public List<string> GetDocumentNames(string sessionId) {
        lock (_lock) {
            return _store.TryGetValue(sessionId, out var chunks) 
                    ? chunks.Select(c => c.FileName).Distinct().ToList()
                    : new List<string>();
        }
    }
    
    public void ClearSession(string sessionId) {
        lock (_lock) {
            _store.Remove(sessionId);
        }
    }

    private static float CosineSimilarity(float[] a, float[] b) {
        if (a.Length != b.Length) return 0f;

        float dot = 0f, magA = 0f, magB = 0f;

        for (int i = 0; i < a.Length; i++) {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }

        var denom = MathF.Sqrt(magA) * MathF.Sqrt(magB);
        return denom == 0f ? 0f : dot / denom;
    }

}