using System.Text.Json.Serialization;

namespace CommunityService.Dtos
{
    public class CheckTextResponseDto
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("is_toxic")]
        public bool IsToxic { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}
