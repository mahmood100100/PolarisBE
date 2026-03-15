using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polaris.Domain.Entities
{
    public class ProjectFile
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsDirectory { get; set; }
        public string Extension { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;

        // Foreign key to Project
        public Guid ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        //self-referencing relationship for parent-child hierarchy
        public Guid? ParentId { get; set; }
        public ProjectFile? Parent { get; set; }
        public ICollection<ProjectFile> Children { get; set; } = new List<ProjectFile>();
    }
}
