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

        public async Task TriggerIngestionAsync(string cloudinaryUrl, Guid documentId)
        {
            try
            {
                var baseUrl = _configuration["Langflow:BaseUrl"];
                var flowId = _configuration["Langflow:IngestionFlowId"];
                var apiKey = _configuration["Langflow:ApiKey"];

                // 1. Download PDF from Cloudinary
                var httpClient = _httpClientFactory.CreateClient();
                var pdfBytes = await httpClient.GetByteArrayAsync(cloudinaryUrl);

                // 2. Extract text using PdfPig
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

                // 3. Send extracted text to Langflow
                var payload = new
                {
                    input_type = "text",
                    output_type = "text",
                    tweaks = new Dictionary<string, object>
                    {
                        ["TextInput-nIOtJ"] = new { input_value = extractedText }
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
                    await langflowDocService.UpdateDocumentStatusAsync(documentId, "Ready");
                    _logger.LogInformation("Document {DocumentId} ingestion completed", documentId);
                }
                else
                {
                    await langflowDocService.UpdateDocumentStatusAsync(documentId, "Error");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Langflow trigger failed for {DocumentId}", documentId);
                try
                {
                    using var errorScope = _serviceScopeFactory.CreateScope();
                    var errorDocService = errorScope.ServiceProvider.GetRequiredService<IDocumentService>();
                    await errorDocService.UpdateDocumentStatusAsync(documentId, "Error");
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
