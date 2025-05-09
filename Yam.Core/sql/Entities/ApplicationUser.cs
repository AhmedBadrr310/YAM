using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yam.Core.sql.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string DisplayName { get; set; } = null!;

        public IReadOnlyList<byte> ProfilePicture { get; set; } = new List<byte>();

        public string? Gender { get; set; }

        public DateOnly BirthDate { get; set; }

        public DateOnly JoinDate { get; set; }

        public string? Bio { get; set; }

        public List<RefreshToken>? RefreshTokens { get; set; }
    }
}
