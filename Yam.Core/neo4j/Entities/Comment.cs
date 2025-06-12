using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yam.Core.neo4j.Entities
{
    public class Comment
    {
        public string CommentId { get; set; } = Guid.NewGuid().ToString();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(2);

        public string Content { get; set; } = null!;

        public string AuthorId { get; set; } = null!;

        public string PostId { get; set; } = null!;

        public int LikesCount { get; set; }
    }
}
