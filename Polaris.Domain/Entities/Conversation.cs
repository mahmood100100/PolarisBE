using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polaris.Domain.Entities
{
    public class Conversation
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = "New Chat";
        public string ModelName { get; set; } = "DeepSeek-V3.2";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign key to Project
        public Guid? ProjectId { get; set; }
        public Project? Project { get; set; }

        // Foreign key to User
        public Guid UserId { get; set; }
        public LocalUser User { get; set; } = null!;

        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
