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
    public class PostController(IPostService postService) : ControllerBase
    {
        private readonly HttpClient _client = new HttpClient();
        private readonly IPostService _postService = postService;

        [HttpPost]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<ActionResult<ApiResponse<object>>> CreatePost([FromForm] PostDtoToGet postDto, string communityId)
        {
            try
            {
                var userId = GetUserId();
                //requesting the community service to check if the user is a member of the community
                var token =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Request.Headers["Authorization"].ToString().Replace("Bearer ", "").Trim());
                
                
                var post = new Post
                {
                    Content = postDto.Content,
                    CommunityId = communityId,
                    CeatorId = userId
                };

                var result = await _postService.CreatePostAsync(post, communityId, userId, token, postDto.Image);
                if (result == null)
                {
                    return BadRequest(new ApiResponse<object> { Code = 400, Message = "Failed to create post." });
                }
                return Ok(new ApiResponse<object> { Code = 200, Message = "Post created successfully.", Data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object> { Code = 400, Message = ex.Message, Data = null });
            }
        }

        [HttpPost("comment")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<ActionResult<ApiResponse<object>>> CreateComment([FromBody]CommentDtoToGet commentDto)
        {
            //try
            //{
            //    var user = GetUserId();

            //    var comment = new Comment
            //    {
            //        PostId = commentDto.PostId,
            //        AuthorId = user,
            //        Content = commentDto.Content
            //    };

            //    var result = await _repository.CreateCommentAsync(comment, commentDto.PostId, user)!;
            //    return Ok(new ApiResponse { Code = 200, Message = "Created successfully", Data = result });
            //}
            //catch (Exception e)
            //{
            //    return BadRequest(new ApiResponse { Code = 400, Message = e.Message, Data = null });
            //}
            return Ok();
        }

        [HttpGet("posts/{communityId}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<ActionResult<ApiResponse<object>>> GetAllPosts(string communityId)
        {
            //try
            //{

            //    var posts = await _repository.GetAllPostsAsync(communityId)!;
            //    if (posts == null)
            //    {
            //        return Ok(new ApiResponse { Code = 200, Message = "No posts found.", Data = null });
            //    }
            //    return Ok(new ApiResponse() { Code = 200, Message = "Success", Data = posts });

            //}
            //catch (Exception e)
            //{
            //    return StatusCode(500, new ApiResponse { Code = 500, Message = e.Message, Data = null });
            //}
            return Ok();
        }

        [HttpGet("comments/{postId}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<ActionResult<ApiResponse<object>>> GetPostComments(string postId)
        {
            //var result = await _repository.GetCommentsByPostIdAsync(postId);
            //return Ok(result);
            return Ok();
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
