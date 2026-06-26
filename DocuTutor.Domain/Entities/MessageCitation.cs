using System;
using System.Collections.Generic;
using System.Text;

namespace DocuTutor.Domain.Entities
{
    public class MessageCitation
    {
        public Guid Id { get; set; }

        public Guid MessageId { get; set; }
        public Message Message { get; set; } = default!;

        public Guid DocumentChunkId { get; set; }
        public DocumentChunk DocumentChunk { get; set; } = default!;

        public int? PageNumber { get; set; }
        public string? QuoteText { get; set; }
        public int Ordinal { get; set; }
    }
}
