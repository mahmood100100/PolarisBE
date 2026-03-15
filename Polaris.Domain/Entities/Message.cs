using Polaris.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polaris.Domain.Entities
{
    public class Message
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public MessageRole Role { get; set; }
        public MessageStatus Status { get; set; } = MessageStatus.Completed;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        // Foreign key to Conversation
        public Guid ConversationId { get; set; }
        public Conversation Conversation { get; set; } = null!;
    }
}
