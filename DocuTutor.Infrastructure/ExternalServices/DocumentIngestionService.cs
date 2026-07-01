using DocuTutor.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using UglyToad.PdfPig;

namespace DocuTutor.Infrastructure.ExternalServices
{
    public class DocumentIngestionService : IDocumentIngestionService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<DocumentIngestionService> _logger;

        public DocumentIngestionService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<DocumentIngestionService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        public async Task TriggerIngestionAsync(string cloudinaryUrl, Guid documentId, string userId)
        {
            try
            {
                var baseUrl = _configuration["Langflow:BaseUrl"]
                    ?? throw new InvalidOperationException("Langflow:BaseUrl is not configured.");
                var flowId = _configuration["Langflow:IngestionFlowId"]
                    ?? throw new InvalidOperationException("Langflow:IngestionFlowId is not configured.");
                var apiKey = _configuration["Langflow:ApiKey"]
                    ?? throw new InvalidOperationException("Langflow:ApiKey is not configured.");
                var textInputNodeId = _configuration["Langflow:IngestionTextInputNodeId"] ?? "TextInput-nIOtJ";
                var metadataNodeId = _configuration["Langflow:IngestionMetadataNodeId"] ?? "CustomComponent-viZPZ";

                var httpClient = _httpClientFactory.CreateClient();
                var pdfBytes = await httpClient.GetByteArrayAsync(cloudinaryUrl);

                var extractedText = ExtractTextFromPdf(pdfBytes);
                _logger.LogInformation("Extracted {Length} characters from PDF", extractedText.Length);

                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    _logger.LogError("No text extracted from PDF for document {DocumentId}", documentId);
                    using var extractScope = _serviceScopeFactory.CreateScope();
                    var extractDocService = extractScope.ServiceProvider.GetRequiredService<IDocumentService>();
                    await extractDocService.UpdateDocumentStatusAsync(documentId, "Error");
                    return;
                }

                var payload = new
                {
                    input_type = "text",
                    output_type = "text",
                    tweaks = new Dictionary<string, object>
                    {
                        [textInputNodeId] = new { input_value = extractedText },
                        [metadataNodeId] = new
                        {
                            document_id = documentId.ToString(),
                            user_id = userId
                        }
                    }
                };

                var runClient = _httpClientFactory.CreateClient();
                runClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
                runClient.Timeout = TimeSpan.FromMinutes(5);

                var response = await runClient.PostAsJsonAsync(
                    $"{baseUrl}/api/v1/run/{flowId}?stream=false",
                    payload);

                var body = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Langflow response [{Status}]: {Body}",
                    response.StatusCode, body);

                using var langflowScope = _serviceScopeFactory.CreateScope();
                var langflowDocService = langflowScope.ServiceProvider.GetRequiredService<IDocumentService>();

                if (response.IsSuccessStatusCode)
                {
                    await langflowDocService.UpdateDocumentStatusAsync(documentId, "Ready", userId);
                    _logger.LogInformation("Document {DocumentId} ingestion completed", documentId);
                }
                else
                {
                    _logger.LogError(
                        "Langflow ingestion failed for document {DocumentId} using text node {TextNodeId} and metadata node {MetadataNodeId}",
                        documentId,
                        textInputNodeId,
                        metadataNodeId);
                    await langflowDocService.UpdateDocumentStatusAsync(documentId, "Error", userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Langflow trigger failed for {DocumentId}", documentId);
                try
                {
                    using var errorScope = _serviceScopeFactory.CreateScope();
                    var errorDocService = errorScope.ServiceProvider.GetRequiredService<IDocumentService>();
                    await errorDocService.UpdateDocumentStatusAsync(documentId, "Error", userId);
                }
                catch (Exception dbEx)
                {
                    _logger.LogError(dbEx, "Failed to update error status for document {DocumentId}", documentId);
                }
            }
        }

        private string ExtractTextFromPdf(byte[] pdfBytes)
        {
            var sb = new System.Text.StringBuilder();

            using var stream = new MemoryStream(pdfBytes);
            using var pdf = PdfDocument.Open(stream);

            foreach (var page in pdf.GetPages())
            {
                sb.AppendLine(page.Text);
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
