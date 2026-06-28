namespace DocuTutor.Application.Exceptions
{
    public class DocumentNotReadyException : Exception
    {
        public DocumentNotReadyException(Guid documentId, string status)
            : base($"Document {documentId} is not ready for retrieval (status: {status}).")
        {
            DocumentId = documentId;
            Status = status;
        }

        public Guid DocumentId { get; }
        public string Status { get; }
    }
}
