# AI Study Assistant

A full-stack AI-powered study tool built with ASP.NET Core and Blazor WebAssembly.
Upload your notes, generate flashcards, get quizzed, or ask questions - all powered
by a from-scratch RAG pipeline.

## Features

- **Streaming AI chat** - real-time token streaming via Server-Sent Events
- **Study modes** - general explanation, flashcard generation, and quiz mode
- **Interactive flashcards** - flip cards with review marking and deck filtering
- **Quiz scoring** - per-session score tracking with automatic answer detection
- **RAG pipeline** - upload .pdf or .txt notes; the AI answers from your documents
- **Vector search** - cosine similarity over HuggingFace embeddings, no external vector DB

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core 8 Web API |
| Frontend | Blazor WebAssembly |
| Language | C# throughout |
| LLM | Groq API (Llama 3.3 70B) |
| Embeddings | HuggingFace Inference API (all-MiniLM-L6-v2) |
| PDF parsing | PdfPig |
| Markdown | Markdig |
| Streaming | Server-Sent Events |

## Architecture

```
StudyAssistant/
├── StudyAssistant.Api/          # ASP.NET Core backend
│   ├── Controllers/             # ChatController, DocumentController, StatsController
│   ├── Models/                  # Domain models
│   └── Services/                # ConversationService, DocumentService,
│                                # EmbeddingService, VectorStore, PromptService
└── StudyAssistant.Web/          # Blazor WASM frontend
    ├── Pages/                   # Index.razor, About.razor
    ├── Components/              # FlashcardDeck.razor
    └── Services/                # FlashcardParser
```

## Running Locally

**Prerequisites:** .NET 8 SDK, Groq API key, HuggingFace API key

```bash
# Clone the repo
git clone https://github.com/BokeChinmay/StudyAssistant.git
cd StudyAssistant

# Store your API keys
cd StudyAssistant.Api
dotnet user-secrets set "Groq:ApiKey" "your-groq-key-here"
dotnet user-secrets set "HuggingFace:ApiKey" "your-hf-key-here"

# Terminal 1 — API
dotnet run

# Terminal 2 — Frontend
cd ../StudyAssistant.Web
dotnet run
```

Then open `http://localhost:5211`.

## How the RAG Pipeline Works

1. **Upload** - PDF or text file is received by the API
2. **Chunk** - Document split into 400-word segments with 80-word overlap
3. **Embed** - Each chunk converted to a 384-dimensional vector via HuggingFace
4. **Store** - Vectors held in an in-memory session store keyed by session ID
5. **Query** - User message embedded, cosine similarity run against all chunks
6. **Inject** - Top 4 matching chunks injected into the LLM system prompt
7. **Respond** - LLM answers grounded in your document content

## Live Demo

Frontend: [bokechinmay.github.io/StudyAssistant](https://bokechinmay.github.io/StudyAssistant)