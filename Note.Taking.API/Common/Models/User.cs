namespace Note.Taking.API.Common.Models
{
    public class User
    {
        public int Id { get; set; }

        public string Email { get; set; } 
        public string PasswordHash { get; set; }
        public string FullName { get; set; }

        public DateTime CreatedAt { get; set; }
        public string? StoredRefreshToken { get; set; }
        public string? StoredAccessToken { get; set; }
        public List<Note> Notes { get; set; }
    }
}
