using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polaris.Infrastructure.DTOs.DeepSeek
{
    public class DeepSeekResponse
    {
        public DeepSeekChoice[]? Choices { get; set; }
        public DeepSeekUsage? Usage { get; set; }
    }
}
