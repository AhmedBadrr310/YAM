using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text.Json.Serialization;

namespace CommunityService.Dtos
{
    public class CommunityDtoToGet
    {
        public string Name { get; set; } = null!;

        public List<byte> Banner { get; set; } = new List<byte>();

        public string? Description { get; set; }

        public bool IsPublic { get; set; }

        [JsonIgnore]
        [BindNever]
        public string? CreatorId { get; set; }

        [JsonIgnore]
        [BindNever]
        public List<string> Members => new List<string> { CreatorId! };
    }
}
