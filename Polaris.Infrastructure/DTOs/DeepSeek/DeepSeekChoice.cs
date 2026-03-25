using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polaris.Infrastructure.DTOs.DeepSeek
{
    public class DeepSeekChoice
    {
        public DeepSeekMessage? Delta { get; set; }
        public DeepSeekMessage? Message { get; set; }
        public string? FinishReason { get; set; }
        public int Index { get; set; }
    }
}
