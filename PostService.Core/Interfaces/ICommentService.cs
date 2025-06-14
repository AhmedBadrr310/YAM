using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yam.Core.neo4j.Entities;

namespace PostService.Core.Interfaces
{
    public interface ICommentService
    {
        Task<Comment> CreateCommentAsync(Comment comment, string postId, string userId);
        Task<Comment> UpdateCommentAsync(string commentId, Comment newComment, string userId);
        Task<bool> DeleteCommentAsync(string commentId, string userId);
        Task<PaginatedResult<Comment>> GetCommentsByPostIdAsync(string postId, int pageIndex, int pageSize);
        Task<bool> LikeOrUnLikeCommentAsync(string commentId, string userId);
    }
}
