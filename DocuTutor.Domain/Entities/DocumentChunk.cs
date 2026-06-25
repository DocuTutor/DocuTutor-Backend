using System;

namespace DocuTutor.Domain.Entities
{
    public class DocumentChunk
    {
        public Guid Id { get; set; }
        public Guid DocumentId { get; set; }
        public int ChunkOrder { get; set; }
        public string ChunkText { get; set; } = string.Empty;
        public float[]? Embedding { get; set; }

        // Navigation property
        public Document Document { get; set; } = null!;
    }
}
