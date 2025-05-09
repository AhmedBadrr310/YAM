using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yam.Core.neo4j.Relationships
{
    public class HasRole
    {
        public string? UserId { get; set; }

        public string? RoleId { get; set; }

        public string? CommunityId { get; set; }
    }
}
