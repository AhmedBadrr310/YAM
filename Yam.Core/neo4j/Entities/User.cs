using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace Yam.Core.neo4j.Entities
{
    public class User
    {
        public string SqlId { get; set; } = null!;

        public string? Username { get; set; }

        public string? Email { get; set; }

    }
}
