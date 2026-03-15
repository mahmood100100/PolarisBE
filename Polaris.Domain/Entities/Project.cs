using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polaris.Domain.Entities
{
    public class Project
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string RootPath { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign key to User
        public Guid UserId { get; set; }

        // Navigation properties
        public ICollection<ProjectFile> Files { get; set; } = new List<ProjectFile>();
        public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();

    }
}
