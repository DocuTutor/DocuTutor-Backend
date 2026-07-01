using DocuTutor.Application.DTOs;
using DocuTutor.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DocuTutor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]

    public class DocumentController : ControllerBase
    {
        private readonly IDocStorageService _docStorageService;
        private readonly IDocumentService _documentService;
        private readonly IDocumentIngestionService _ingestionService;
        private readonly ILogger<DocumentController> _logger;

        public DocumentController(
            IDocStorageService docStorageService,
            IDocumentService documentService,
            IDocumentIngestionService ingestionService,
            ILogger<DocumentController> logger)
        {
            _docStorageService = docStorageService;
            _documentService = documentService;
            _ingestionService = ingestionService;
            _logger = logger;
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadDocument(IFormFile file)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { message = "User is not authenticated" });

            if (file == null)
                return BadRequest(new { message = "File is required" });

            if (!file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Only PDF files are accepted" });

            try
            {
                _logger.LogInformation("Uploading file: {FileName}", file.FileName);

                var cloudinaryResult = await _docStorageService.UploadDocumentAsync(file);
                var document = await _documentService.CreateDocumentAsync(
                    fileName: file.FileName,
                    cloudinaryUrl: cloudinaryResult.Url,
                    userId: userId
                );

                _ = _ingestionService.TriggerIngestionAsync(cloudinaryResult.Url, document.Id, userId);

                return Ok(new DocumentUploadResponseDto
                {
                    DocumentId = document.Id,
                    FileName = document.FileName,
                    CloudinaryUrl = document.CloudinaryUrl,
                    Status = document.Status,
                    CreatedAt = document.CreatedAt
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Validation error: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document");
                return StatusCode(500, new { message = "An error occurred while processing your request" });
            }
        }

        [HttpGet("{documentId}")]
        public async Task<IActionResult> GetDocument(Guid documentId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(userId))
                    return Unauthorized(new { message = "User is not authenticated" });

                var document = await _documentService.GetDocumentByIdAsync(documentId, userId);
                if (document == null)
                    return NotFound(new { message = "Document not found" });

                return Ok(new DocumentUploadResponseDto
                {
                    DocumentId = document.Id,
                    FileName = document.FileName,
                    CloudinaryUrl = document.CloudinaryUrl,
                    Status = document.Status,
                    CreatedAt = document.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving document {DocumentId}", documentId);
                return StatusCode(500, new { message = "An error occurred while processing your request" });
            }
        }
    }
}