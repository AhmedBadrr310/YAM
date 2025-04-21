using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace Yam.Core.Entities
{
    public class User
    {
        public string? Username { get; set; }

        public string? Name { get; set; }

        public string? Email { get; set; }

        public string? PasswordHashed { get; set; }

        public IReadOnlyList<byte> ProfilePicture { get; set; } = new List<byte>();

        public string? Gender { get; set; }

        public DateOnly BirthDate{ get; set; }

        public DateOnly JoinDate { get; set; }

        public string? Bio { get; set; }
    }
}
