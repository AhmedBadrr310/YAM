using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostService.Core.Interfaces;
using PostService.Dtos;
using PostService.Responses;
using System.Text.Json;
using Yam.Core.neo4j.Entities;

namespace PostService.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class PostController(IPostRepository repository) : ControllerBase
    {
        private readonly IPostRepository _repository = repository;
        private readonly string CommunitySerivceUrl = "https://localhost:7041/api/Commuity";
        private readonly HttpClient _client = new HttpClient();

        [HttpPost]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<ActionResult<ApiResponse>> CreatePost([FromBody] PostDtoToGet postDto, string communityId)
        {
            var userId = GetUserId();
            //requesting the community service to check if the user is a member of the community
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Request.Headers["Authorization"].ToString().Replace("Bearer ", "").Trim());
            var response = await _client.GetAsync(CommunitySerivceUrl);

            //Converting the response to a string
            var stringResponse = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var communities = JsonSerializer.Deserialize<List<Community>>(stringResponse, options);

            if (communities is null || !communities.Any())
            {
                return Unauthorized(new ApiResponse { Code = 401, Message = "User is not authorized", Data = null });
            }

            bool isAuthorized = false;
            foreach (var community in communities)
            {
                if (community.CommunityId == communityId)
                {
                    isAuthorized = true;
                    break;
                }
            }
            if (!isAuthorized)
            {
                return Unauthorized(new ApiResponse { Code = 401, Message = "User is not authorized", Data = null });
            }

            var post = new Post
            {
                Image = postDto.Image,
                Content = postDto.Content,
                CommunityId = communityId,
                CeatorId = userId
            };

            var result = await _repository.CreatePostAsync(post, communityId, userId);
            if (result == null)
            {
                return BadRequest(new ApiResponse { Code = 400, Message = "Failed to create post." });
            }
            return Ok(new ApiResponse { Code = 200, Message = "Post created successfully.", Data = result });
        }

        [HttpPost("comment")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<ActionResult<ApiResponse>> CreateComment([FromBody]CommentDtoToGet commentDto)
        {
            try
            {
                var user = GetUserId();

                var comment = new Comment
                {
                    PostId = commentDto.PostId,
                    AuthorId = user,
                    Content = commentDto.Content
                };

                var result = await _repository.CreateCommentAsync(comment, commentDto.PostId, user)!;
                return Ok(new ApiResponse { Code = 200, Message = "Created successfully", Data = result });
            }
            catch (Exception e)
            {
                return BadRequest(new ApiResponse { Code = 400, Message = e.Message, Data = null });
            }
        }

        [HttpGet("posts/{communityId}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<ActionResult<ApiResponse>> GetAllPosts(string communityId)
        {
            try
            {

                var posts = await _repository.GetAllPostsAsync(communityId)!;
                if (posts == null)
                {
                    return Ok(new ApiResponse { Code = 200, Message = "No posts found.", Data = null });
                }
                return Ok(new ApiResponse() { Code = 200, Message = "Success", Data = posts });

            }
            catch (Exception e)
            {
                return StatusCode(500, new ApiResponse { Code = 500, Message = e.Message, Data = null });
            }
        }

        [HttpGet("comments/{postId}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<ActionResult<ApiResponse>> GetPostComments(string postId)
        {
            var result = await _repository.GetCommentsByPostIdAsync(postId);
            return Ok(result);
        }



        private string GetUserId()
        {
            var userId = User.Claims.Where(User => User.Type == "user-id")
                .Select(User => User.Value)
                .FirstOrDefault();

            return userId!;
        }
    }
}
