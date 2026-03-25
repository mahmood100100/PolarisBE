using MediatR;
using Polaris.Domain.Interfaces.IRepositories;

namespace Polaris.Application.Features.Conversations.Queries.GetActiveChatJobs
{
    public class GetActiveChatJobsHandler : IRequestHandler<GetActiveChatJobsQuery, List<ActiveChatJobDto>>
    {
        private readonly IGenerationJobRepository _jobRepository;

        public GetActiveChatJobsHandler(IGenerationJobRepository jobRepository)
        {
            _jobRepository = jobRepository;
        }

        public async Task<List<ActiveChatJobDto>> Handle(GetActiveChatJobsQuery request, CancellationToken cancellationToken)
        {
            var activeJobs = await _jobRepository.GetActiveJobsByUserIdAsync(request.UserId, cancellationToken);
            
            // Return only jobs that are linked to a specific Chat Conversation
            return activeJobs
                .Where(j => j.ConversationId.HasValue)
                .Select(j => new ActiveChatJobDto
                {
                    JobId = j.Id,
                    ConversationId = j.ConversationId!.Value
                })
                .ToList();
        }
    }
}
