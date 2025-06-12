using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yam.Core.neo4j.Entities
{
    public class Community
    {
        public string CommunityId { get; set; } = null!;

        public string Name { get; set; } = null!;
        
        public List<byte> Banner { get; set; } = new List<byte>();
        
        public string? Description { get; set; }

        public bool IsDeleted { get; set; } = false;

        public bool IsPublic { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(2);

        public string CreatorId { get; set; } = null!;

        public List<string> Members { get; set; } = new List<string>();
    }
}
