using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yam.AuthService.Responses;
using Yam.Core.neo4j.Entities;

namespace PostService.Core.Interfaces
{
    public interface IPostRepository
    {
        Task<Post>? CreatePostAsync(Post post, string communityId, string userId);
        Task<Comment>? CreateCommentAsync(Comment comment, string postId, string userId);
        Task<bool> UpdatePostAsync(string postId, Post newPost);
        Task<bool> DeletePostAsync(string postId);
        Task<List<Post>>? GetPostsByUserIdAsync(string userId);
        Task<List<Comment>>? GetCommentsByPostIdAsync(string postId);
        Task<List<Post>>? GetAllPostsAsync(string communityId);
    }
}
