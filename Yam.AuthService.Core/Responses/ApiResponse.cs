using System.Text.Json.Serialization;

namespace Yam.AuthService.Responses
{
    public class ApiResponse
    {
        public int Code  { get; set; }

        public string Messasge { get; set; } = null!;

        public string UserId { get; set; }

        public string Token { get; set; } = null!;

        public DateTime ExpirationDate { get; set; }

        [JsonIgnore]
        public string RefreshToken { get; set; }

        public DateTime RefreshTokenExpirationDate { get; set; }
    }
}
