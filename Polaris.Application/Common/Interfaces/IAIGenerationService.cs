using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polaris.Application.Common.Interfaces
{
    public interface IAIGenerationService
    {
        Task<string> GenerateCodeAsync(string prompt, Guid userId);
    }
}
