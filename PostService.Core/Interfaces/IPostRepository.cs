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
        Task<Post> UpdatePostAsync(string postId, Post newPost);
        Task<bool> DeletePostAsync(string postId);
        Task<List<Post>>? GetPostsByUserIdAsync(string userId);
        Task<PaginatedResult<Post>>? GetAllPostsAsync(string communityId, string? searchValue, string? sort, int pageNumber, int pageSize);
        Task<Post>? GetPostById(string postId);
        Task LikePostAsync(string postId, string userId);
        Task UnLikePostAsync(string postId, string userId);
        Task<User> CheckUserLikeAsync(string postId, string userId);
    }
}
