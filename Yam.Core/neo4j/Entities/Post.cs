using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yam.Core.neo4j.Entities
{
    public class Post
    {
        public string PostId { get; set; } = Guid.NewGuid().ToString();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(2);

        public string CeatorId { get; set; } = null!;

        public string ImageUrl { get; set; } = null!;

        public string Content { get; set; } = null!;

        public int LikesCount { get; set; } = 0;

        public int CommentsCount { get; set; } = 0;

        public string CommunityId { get; set; } = null!;
    }
}
