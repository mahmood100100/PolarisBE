namespace Polaris.WebAPI.Models.User
{
    public class UserListItemResponse
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
    }
}
