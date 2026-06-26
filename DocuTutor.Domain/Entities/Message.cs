using DocuTutor.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace DocuTutor.Domain.Entities
{
    public class Message
    {
        public Guid Id { get; set; }

        public Guid ConversationId { get; set; }
        public Conversation Conversation { get; set; } = default!;

        public MessageRole Role { get; set; }
        public string Content { get; set; } = default!;
        public MessageStatus Status { get; set; }


        public DateTimeOffset CreatedAt { get; set; }

        public ICollection<MessageCitation> Citations { get; set; } = new List<MessageCitation>();
        
    }
}
