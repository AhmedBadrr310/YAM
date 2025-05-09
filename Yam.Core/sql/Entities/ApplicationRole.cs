using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yam.Core.sql.Entities
{
    public class ApplicationRole : IdentityRole
    {
        public string CommunityId { get; set; } = null!;
    }
}
