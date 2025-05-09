using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yam.Core.sql.Entities
{
    [Owned]
    public class RefreshToken
    {
        public string Token { get; set; }

        public DateTime ExpiresAt { get; set; }

        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

        public DateTime CreatedAt { get; set; }

        public DateTime? RevokedAt { get; set; }

        public bool IsActive => RevokedAt is null && !IsExpired;
    }
}
