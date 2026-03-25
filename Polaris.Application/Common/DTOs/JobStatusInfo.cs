using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polaris.Application.Common.DTOs
{
    public class JobStatusInfo
    {
        public bool Exists { get; set; }
        public string? State { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? Error { get; set; }
    }
}
