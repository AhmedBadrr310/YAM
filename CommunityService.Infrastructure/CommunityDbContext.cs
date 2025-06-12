using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Yam.Core.sql;
using Yam.Core.sql.Entities;

namespace CommunityService.Infrastructure
{
    public class CommunityDbContext : IdentityDbContext<ApplicationUser,ApplicationRole,string>
    {
        public CommunityDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

        }
    }
}
