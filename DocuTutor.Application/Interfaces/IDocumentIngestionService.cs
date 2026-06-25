namespace DocuTutor.Application.Interfaces
{
    public interface IDocumentIngestionService
    {
        /// <summary>
        /// Triggers asynchronous document ingestion: downloads PDF, extracts text, sends to Langflow
        /// </summary>
        Task TriggerIngestionAsync(string cloudinaryUrl, Guid documentId);
    }
}
