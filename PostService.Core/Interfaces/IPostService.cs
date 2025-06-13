using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using Yam.Core.neo4j.Entities;

namespace PostService.Core.Interfaces
{
    public interface IPostService
    {
        Task<Post>? CreatePostAsync(Post post, string communityId, string userId, AuthenticationHeaderValue token, IFormFile image);
        Task<Post> UpdatePostAsync(string postId, Post newPost);
        Task<bool> DeletePostAsync(string postId);
        Task<List<Post>>? GetPostsByUserIdAsync(string userId);
        Task<List<Post>>? GetAllPostsAsync(string communityId);
    }
}
