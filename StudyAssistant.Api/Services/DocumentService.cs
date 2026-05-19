using StudyAssistant.Api.Models;

namespace StudyAssistant.Api.Services;

public class DocumentService {
    private readonly DocumentChunkingService _chunker;
    private readonly EmbeddingService _embedder;
    private readonly VectorStore _vectorStore;

    public DocumentService(DocumentChunkingService chunker, EmbeddingService embedder, VectorStore vectorStore) {
        _chunker = chunker;
        _embedder = embedder;
        _vectorStore = vectorStore;
    }

    public async Task<int> IngestDocumentAsync(string sessionId, string fileName, Stream fileStream) {
        //Step 1: Extract text
        var text = fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
                    ? _chunker.ExtractTextFromPdf(fileStream)
                    : _chunker.ExtractTextFromTxt(fileStream);
        
        if(string.IsNullOrWhiteSpace(text)) {
            throw new InvalidOperationException("No text could be extracted from the document.");
        }

        //Step 2: Chunk text
        var docId = Guid.NewGuid().ToString();
        var chunks = _chunker.ChunkText(docId, fileName, text);

        //Step 3: Embed all chunks
        var texts = chunks.Select(c => c.Text).ToList();
        var embeddings = await _embedder.GetEmbeddingsBatchAsync(texts);

        for (int i = 0; i < chunks.Count; i++) {
            chunks[i].Embedding = embeddings[i];
        }

        //Step 4: Store in vector store
        _vectorStore.AddChunks(sessionId, chunks);

        return chunks.Count;
    }

    public async Task<List<RetrievedContext>> QueryAsync(string sessionId, string query) {
        var queryEmbedding = await _embedder.GetEmbeddingAsync(query);
        return _vectorStore.Search(sessionId, queryEmbedding);
    }

    public List<string> GetDocumentNames(string sessionId) {
        return _vectorStore.GetDocumentNames(sessionId);
    }
}