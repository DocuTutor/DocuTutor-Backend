namespace DocuTutor.Application.Exceptions
{
    public class DocumentNotFoundException : Exception
    {
        public DocumentNotFoundException(Guid documentId)
            : base($"Document {documentId} was not found.")
        {
            DocumentId = documentId;
        }

        public Guid DocumentId { get; }
    }
}
