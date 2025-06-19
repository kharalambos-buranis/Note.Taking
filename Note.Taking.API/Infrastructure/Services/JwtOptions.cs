namespace Note.Taking.API.Infrastructure.Services
{
    public class JwtOptions
    {
        public string Secret { get; set; }
        public string Audience { get; set; }
        public string Issuer { get; set; }
        public int ExpirationInMinutes { get; set; }
    }
}
