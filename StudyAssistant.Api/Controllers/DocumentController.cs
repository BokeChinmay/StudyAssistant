using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Features;
using StudyAssistant.Api.Services;

[ApiController]
[Route("api/[controller]")]
public class DocumentController : ControllerBase {
    private readonly DocumentService _documentService;

    public DocumentController(DocumentService documentService) {
        _documentService = documentService;
    }

    [HttpPost("upload")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> Upload() {
        if(!Request.HasFormContentType) {
            return BadRequest("Expected multipart form data");
        }

        var form = await Request.ReadFormAsync();
        var sessionId = form["sessionId"].ToString();
        var file = form.Files.GetFile("file");

        if(string.IsNullOrEmpty(sessionId)) {
            return BadRequest("Session ID is required");
        }

        if(file == null || file.Length == 0) {
            return BadRequest("No file uploaded");
        }

        var allowed = new[] { ".pdf", ".txt" };
        var ext = Path.GetExtension(file.FileName).ToLower();

        if(!allowed.Contains(ext)) {
            return BadRequest("Only PDF and TXT files are supported");
        }

        if(file.Length > 10 * 1024 * 1024) {
            return BadRequest("File must be under 10 MB");
        }

        try {
            using var stream = file.OpenReadStream();
            var chunkCount = await _documentService.IngestDocumentAsync(sessionId, file.FileName, stream);

            return Ok(new {fileName = file.FileName, chunkCount, message = $"Ingested {chunkCount} chunks from {file.FileName}"});
        }
        catch(Exception ex) {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet("list/{sessionId}")]
    public IActionResult ListDocuments(string sessionId) {
        var docs = _documentService.GetDocumentNames(sessionId);
        return Ok(docs);
    }

    [HttpGet("ping")]
    public IActionResult Ping() => Ok("document controller alive");
}