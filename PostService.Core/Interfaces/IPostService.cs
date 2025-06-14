using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using Yam.Core.neo4j.Entities;

namespace PostService.Core.Interfaces
{
    public interface IPostService
    {
        Task<Post>? CreatePostAsync(Post post, string communityId, string userId, AuthenticationHeaderValue token, IFormFile image);
        Task<Post> UpdatePostAsync(string postId, Post newPost, string userId, IFormFile imageFile);
        Task<bool> DeletePostAsync(string postId, string userId);
        Task<List<Post>>? GetPostsByUserIdAsync(string userId);
        Task<PaginatedResult<Post>>? GetAllPostsAsync(string communityId, string? sort, string? search, int pageNumber, int pageSize);
        Task<bool> LikeOrUnLikePostAsync(string postId, string userId);
    }
}
