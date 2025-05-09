namespace Yam.AuthService.Helper
{
    public class JwtOptions
    {
        public string issuer { get; set; }

        public string audience { get; set; }

        public string secretKey { get; set; }

        public int durationInMinutes { get; set; }
    }
}
