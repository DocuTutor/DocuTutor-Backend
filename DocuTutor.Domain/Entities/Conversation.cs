using System;
using System.Collections.Generic;
using System.Text;

namespace DocuTutor.Domain.Entities
{
    public class Conversation
    {
        public Guid Id { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; } = default!;

        public Guid DocumentId { get; set; }
        public Document Document { get; set; } = default!;

        public string? Title { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
