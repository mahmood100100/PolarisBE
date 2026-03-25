using System.Threading;
using System.Threading.Tasks;

namespace Polaris.Application.Common.Interfaces
{
    public interface IFirecrawlService
    {
        Task<string?> ScrapeUrlAsync(string url, CancellationToken cancellationToken = default);
        Task<string?> SearchWebAsync(string query, CancellationToken cancellationToken = default);
    }
}
