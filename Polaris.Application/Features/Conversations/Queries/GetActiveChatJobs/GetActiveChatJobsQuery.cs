using MediatR;

namespace Polaris.Application.Features.Conversations.Queries.GetActiveChatJobs
{
    public class GetActiveChatJobsQuery : IRequest<List<ActiveChatJobDto>>
    {
        public Guid UserId { get; set; }
    }

    public class ActiveChatJobDto
    {
        public Guid JobId { get; set; }
        public Guid ConversationId { get; set; }
    }
}
