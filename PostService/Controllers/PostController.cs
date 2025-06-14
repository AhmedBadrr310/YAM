using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostService.Core;
using PostService.Core.Interfaces;
using PostService.Dtos;
using PostService.Responses;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Yam.Core.neo4j.Entities;

namespace PostService.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class PostController(IPostService postService) : ControllerBase
    {
        private readonly HttpClient _client = new HttpClient();
        private readonly IPostService _postService = postService;

        [HttpPost]
        public async Task<ActionResult<ApiResponse<object>>> CreatePost([FromForm] PostDtoToGet postDto, string communityId)
        {
            try
            {
                var userId = GetUserId();
                //requesting the community service to check if the user is a member of the community
                var token = System.Net.Http.Headers.AuthenticationHeaderValue.Parse(Request.Headers["Authorization"]);


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

        //[HttpPost("comment")]
        //public async Task<ActionResult<ApiResponse<object>>> CreateComment([FromBody]CommentDtoToGet commentDto)
        //{
        //    //try
        //    //{
        //    //    var user = GetUserId();

        //    //    var comment = new Comment
        //    //    {
        //    //        PostId = commentDto.PostId,
        //    //        AuthorId = user,
        //    //        Content = commentDto.Content
        //    //    };

        //    //    var result = await _repository.CreateCommentAsync(comment, commentDto.PostId, user)!;
        //    //    return Ok(new ApiResponse { Code = 200, Message = "Created successfully", Data = result });
        //    //}
        //    //catch (Exception e)
        //    //{
        //    //    return BadRequest(new ApiResponse { Code = 400, Message = e.Message, Data = null });
        //    //}
        //    return Ok();
        //}

        [HttpGet("{communityId}")]
        public async Task<ActionResult<ApiResponse<object>>> GetAllPosts(string communityId, [FromQuery] GetPostsParamsDto paramsDto) // Default to 10 items per page  
        {
            try
            {
                var paginatedPosts = await _postService.GetAllPostsAsync(
                    communityId,
                    paramsDto.sort,
                    paramsDto.search,
                    paramsDto.pageNumber,
                    paramsDto.pageSize
                );

                // The Data property now holds our PaginatedResult object  
                return Ok(new ApiResponse<PaginatedResult<Post>> { Code = 200, Message = "Success", Data = paginatedPosts });
            }
            catch (Exception e)
            {
                return StatusCode(500, new ApiResponse<object> { Code = 500, Message = e.Message, Data = null });
            }
        }


        [HttpGet()]
        public async Task<ActionResult<ApiResponse<List<Post>>>> GetPostsByUserId(string? userId)
        {
            try
            {
                string existingUserId;
                if (!string.IsNullOrEmpty(userId))
                {
                    existingUserId = userId;
                }
                else
                {
                    existingUserId = GetUserId();
                }

                var posts = await _postService.GetPostsByUserIdAsync(existingUserId);

                return Ok(new ApiResponse<object>() { Code = 200, Message = "Success", Data = posts });

            }
            catch (Exception ex )
            {
                return BadRequest(new ApiResponse<object> { Code = 400, Message = ex.Message, Data = null });
            }

        }

        [HttpPut]
        public async Task<ActionResult<ApiResponse<Post>>> UpdatePost([FromQuery] string postId, [FromForm] PostDtoToGet dto)
        {
            try
            {
                var userId = GetUserId();
                var newPost = new Post
                {
                    CeatorId = userId,
                    Content = dto.Content
                };
                var result = await _postService.UpdatePostAsync(postId, newPost, userId, dto.Image);

                return Ok(new ApiResponse<Post> { Code = 200, Message = "Success", Data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object> { Code = 200, Message = ex.Message, Data = null });
            }
        }


        [HttpDelete("{postId}")]
        public async Task<ActionResult<ApiResponse<object>>> DeletePost(string postId)
        {
            try
            {
                var userId = GetUserId();

                var result = await _postService.DeletePostAsync(postId, userId);
                if (!result)
                    return Unauthorized(new ApiResponse<object> { Code = 401, Message = "User is Not Authorized", Data = null });

                return Ok(new ApiResponse<object> { Code = 200, Message = "Success", Data = null });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object> { Code = 400, Message = ex.Message, Data = null });
            }
        }

        [HttpPost("like/{postId}")]
        public async Task<ActionResult<ApiResponse<object>>> LikeOrUnlikePost(string postId)
        {
            var userId = GetUserId();
            var likeResult = await _postService.LikeOrUnLikePostAsync(postId, userId);
            if(likeResult)
            {
                return Ok(new ApiResponse<bool> { Code = 200, Message = "Liked", Data = true });
            }

            return Ok(new ApiResponse<bool> { Code = 200, Message = "Unliked", Data = false });
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
