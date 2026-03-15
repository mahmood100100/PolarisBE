namespace Polaris.WebAPI.Models.User
{
    public class UserDetailsResponse
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
        public string? UpdatedAt { get; set; }
        public List<string> Roles { get; set; } = new();
        public int ProjectsCount { get; set; }
        public int ConversationsCount { get; set; }
    }
}
