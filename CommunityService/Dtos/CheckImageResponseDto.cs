using System.Text.Json.Serialization;

namespace CommunityService.Dtos
{
    public class Prediction
    {
        [JsonPropertyName("class_name")]
        public string? ClassName { get; set; }

        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }

        [JsonPropertyName("is_harmful")]
        public bool IsHarmful { get; set; }
    }

    public class CheckImageResponseDto
    {
        [JsonPropertyName("prediction")]
        public Prediction? Prediction { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}
