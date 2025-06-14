using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yam.Core.neo4j.Entities;

namespace PostService.Core.Interfaces
{
    public interface ICommentRepository
    {
        Task<Comment> CreateCommentAsync(Comment comment, string postId, string userId);
        Task<Comment> UpdateCommentAsync(string commentId, Comment newComment, string userId);
        Task<Comment> DeleteCommentAsync(string commentId, string userId);
        Task<List<Comment>> GetCommentsByPostIdAsync(string postId, int pageIndex, int pageSize);
        Task<Comment> GetCommentByIdAsync(string commentId);
        Task<User> CheckForLikeAsync(string commentId, string userId);
        Task LikeCommentAsync(string commentId, string userId);
        Task UnLikeCommentAsync(string commentId, string userId);
        Task<long> GetCountAsync(string postId);
    }
}
