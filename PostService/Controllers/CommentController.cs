using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PostService.Core;
using PostService.Core.Interfaces;
using PostService.Dtos;
using PostService.Responses;
using System.ComponentModel.DataAnnotations;
using Yam.Core.neo4j.Entities;

namespace PostService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class CommentController(ICommentService commentService) : ControllerBase
    {
        private readonly ICommentService _commentService = commentService;

        [HttpPost]
        public async Task<ActionResult<ApiResponse<Comment>>> CreateComment(CommentDtoToGet dto)
        {
            try
            {
                var result = await _commentService.CreateCommentAsync(new Comment
                {
                    Content = dto.Content,
                    AuthorId = GetUserId(),
                    PostId = dto.PostId
                }, dto.PostId, GetUserId());


                return Ok(new ApiResponse<Comment>
                {
                    Code = 200,
                    Message = "Comment created successfully.",
                    Data = result
                });
            }
            catch (Exception ex)
            {

                return BadRequest(new ApiResponse<object> { Code = 400, Message = ex.Message, Data = null });
            }
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<PaginatedResult<Comment>>>> GetCommentsByPostId([Required]string postId, int? pageSize, int? pageIndex )
        {
            try
            {
                if(pageSize is null)
                {
                    pageSize = 10;
                }
                if(pageIndex is null)
                {
                    pageIndex = 1;
                }
                var result = await _commentService.GetCommentsByPostIdAsync(postId, pageIndex.Value, pageSize.Value);
                return Ok(new ApiResponse<PaginatedResult<Comment>>
                {
                    Code = 200,
                    Message = "Comments retrieved successfully.",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object> { Code = 400, Message = ex.Message, Data = null });
            }
        }

        [HttpDelete("{commentId}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteComment(string commentId)
        {
            try
            {

                var userId = GetUserId();
                await _commentService.DeleteCommentAsync(commentId, userId);

                return Ok(new ApiResponse<object> { Code = 200, Message = "Success", Data = null });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object> { Code = 400, Message = ex.Message, Data = null });
            }
        }

        [HttpPost("like/{commentId}")]
        public async Task<ActionResult<ApiResponse<object>>> LikeOrUnLikePost(string commentId)
        {
            try
            {
                var userId = GetUserId();
                var result = await _commentService.LikeOrUnLikeCommentAsync(commentId, userId);
                return Ok(new ApiResponse<object>
                {
                    Code = 200,
                    Message = result ? "Comment liked successfully." : "Comment unliked successfully.",
                    Data = null
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object> { Code = 400, Message = ex.Message, Data = null });
            }
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
