using System;

namespace DocuTutor.Domain.Entities
{
    public class Document
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string CloudinaryUrl { get; set; } = string.Empty;
        public string Status { get; set; } = "processing"; // processing, ready, error
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
    }
}
