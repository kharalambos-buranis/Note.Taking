namespace Note.Taking.API.Infrastructure.Middleware
{
    public class JwtOptions
    {
        public string Secret { get; set; }
        public string Audience { get; set; }
        public string Issuer { get; set; }
        public int ExpirationInMinutes { get; set; }
    }
}
