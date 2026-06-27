using DocuTutor.Application.DTOs;
using DocuTutor.Application.Exceptions;
using DocuTutor.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace DocuTutor.Infrastructure.ExternalServices
{
    public class LangflowRetrievalService : IRetrievalService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IDocumentService _documentService;
        private readonly ILogger<LangflowRetrievalService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public LangflowRetrievalService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IDocumentService documentService,
            ILogger<LangflowRetrievalService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _documentService = documentService;
            _logger = logger;
        }

        public async Task<AnswerResultDto> AnswerAsync(
            Guid documentId,
            string question,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(question))
                throw new LangflowRetrievalException("Question cannot be empty.");

            var document = await _documentService.GetDocumentByIdAsync(documentId);
            if (document is null)
                throw new DocumentNotFoundException(documentId);

            if (!string.Equals(document.Status, "Ready", StringComparison.OrdinalIgnoreCase))
                throw new DocumentNotReadyException(documentId, document.Status);

            var baseUrl = _configuration["Langflow:BaseUrl"]
                ?? throw new LangflowRetrievalException("Langflow:BaseUrl is not configured.");
            var flowId = _configuration["Langflow:RetrieverFlowId"]
                ?? throw new LangflowRetrievalException("Langflow:RetrieverFlowId is not configured.");
            var apiKey = _configuration["Langflow:ApiKey"]
                ?? throw new LangflowRetrievalException("Langflow:ApiKey is not configured.");

            var chatInputNodeId = _configuration["Langflow:RetrieverChatInputNodeId"]
                ?? _configuration["Langflow:RetrieverQuestionNodeId"]
                ?? "ChatInput-XXXX";
            var retrieverNodeId = _configuration["Langflow:RetrieverComponentNodeId"]
                ?? _configuration["Langflow:RetrieverDocumentIdNodeId"]
                ?? "CustomComponent-XXXX";

            var payload = new
            {
                input_type = "chat",
                output_type = "chat",
                tweaks = new Dictionary<string, object>
                {
                    [chatInputNodeId] = new { input_value = question },
                    [retrieverNodeId] = new
                    {
                        document_id = documentId.ToString(),
                        search_query = question
                    }
                }
            };

            var client = _httpClientFactory.CreateClient("LangflowRetrieval");
            client.DefaultRequestHeaders.Remove("x-api-key");
            client.DefaultRequestHeaders.Add("x-api-key", apiKey);

            HttpResponseMessage response;
            try
            {
                response = await client.PostAsJsonAsync(
                    $"{baseUrl.TrimEnd('/')}/api/v1/run/{flowId}?stream=false",
                    payload,
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Langflow retrieval unavailable for document {DocumentId}", documentId);
                throw new LangflowUnavailableException("Langflow retrieval service is unavailable.", ex);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Langflow retrieval timed out for document {DocumentId}", documentId);
                throw new LangflowUnavailableException("Langflow retrieval service timed out.", ex);
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation(
                "Langflow retrieval response [{Status}] for document {DocumentId}",
                response.StatusCode,
                documentId);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Langflow retrieval failed for document {DocumentId}: {Body}",
                    documentId,
                    body);
                throw new LangflowRetrievalException(
                    $"Langflow retrieval failed with status {(int)response.StatusCode}.");
            }

            return ParseAnswerResult(body);
        }

        private AnswerResultDto ParseAnswerResult(string responseBody)
        {
            var outputText = ExtractOutputText(responseBody);
            if (string.IsNullOrWhiteSpace(outputText))
                throw new LangflowRetrievalException("Langflow returned an empty answer.");

            var trimmed = outputText.Trim();
            if (trimmed.StartsWith('{') && trimmed.EndsWith('}'))
            {
                try
                {
                    var structured = JsonSerializer.Deserialize<StructuredAnswer>(trimmed, JsonOptions);
                    if (structured is not null && !string.IsNullOrWhiteSpace(structured.Answer))
                    {
                        return new AnswerResultDto
                        {
                            Answer = structured.Answer,
                            Sources = structured.Sources ?? []
                        };
                    }
                }
                catch (JsonException)
                {
                    // Fall through and treat the raw text as the answer.
                }
            }

            return new AnswerResultDto
            {
                Answer = outputText,
                Sources = []
            };
        }

        private static string ExtractOutputText(string responseBody)
        {
            using var document = JsonDocument.Parse(responseBody);
            var root = document.RootElement;

            if (!root.TryGetProperty("outputs", out var outputs) || outputs.ValueKind != JsonValueKind.Array)
                return string.Empty;

            foreach (var runOutput in outputs.EnumerateArray())
            {
                if (!runOutput.TryGetProperty("outputs", out var componentOutputs)
                    || componentOutputs.ValueKind != JsonValueKind.Array)
                continue;

                foreach (var componentOutput in componentOutputs.EnumerateArray())
                {
                    var text = TryGetMessageText(componentOutput);
                    if (!string.IsNullOrWhiteSpace(text))
                        return text;
                }
            }

            return string.Empty;
        }

        private static string TryGetMessageText(JsonElement componentOutput)
        {
            if (componentOutput.TryGetProperty("results", out var results)
                && results.TryGetProperty("message", out var message))
            {
                if (message.TryGetProperty("text", out var textElement)
                    && textElement.ValueKind == JsonValueKind.String)
                    return textElement.GetString() ?? string.Empty;

                if (message.TryGetProperty("data", out var data)
                    && data.TryGetProperty("text", out var dataText)
                    && dataText.ValueKind == JsonValueKind.String)
                    return dataText.GetString() ?? string.Empty;
            }

            if (componentOutput.TryGetProperty("artifacts", out var artifacts)
                && artifacts.TryGetProperty("message", out var artifactMessage)
                && artifactMessage.ValueKind == JsonValueKind.String)
                return artifactMessage.GetString() ?? string.Empty;

            if (componentOutput.TryGetProperty("messages", out var messages)
                && messages.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in messages.EnumerateArray())
                {
                    if (item.TryGetProperty("message", out var itemMessage)
                        && itemMessage.ValueKind == JsonValueKind.String)
                        return itemMessage.GetString() ?? string.Empty;
                }
            }

            return string.Empty;
        }

        private sealed class StructuredAnswer
        {
            public string Answer { get; set; } = string.Empty;
            public List<string>? Sources { get; set; }
        }
    }
}
