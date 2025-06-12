using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yam.Core.neo4j.Relationships
{
    public class CommunityMembership
    {
        public DateTime JoinedAt => DateTime.UtcNow.AddHours(2);

        public string MemberType { get; set; } = null!;

        public string Status { get; set; } = null!;
    }
}
