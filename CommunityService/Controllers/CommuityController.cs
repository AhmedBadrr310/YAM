using CommunityService.Core.Interfaces;
using CommunityService.Dtos;
using CommunityService.Respones;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Yam.Core.neo4j.Entities;

namespace CommunityService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommuityController(ICommunityService service, ICommunityRepo repo) : ControllerBase
    {
        private readonly ICommunityService _service = service;
        private readonly ICommunityRepo _repo = repo;

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpPost("create")]
        public async Task<ActionResult<ApiResponse>> CreateCommunity([FromBody] CommunityDtoToGet mdl)
        {
            try
            {
                //get the user-id from the token
                var userId = GetUserId();
                //set the userId to the creatorId so it can be added to the community members
                mdl.CreatorId = userId;
                // Call the service to create the community
                var community = new Community()
                {
                    CommunityId = Guid.NewGuid().ToString(), // Generate a new unique GUID
                    Name = mdl.Name,
                    Banner = mdl.Banner,
                    Description = mdl.Description,
                    IsDeleted = false,
                    IsPublic = mdl.IsPublic,
                    CreatorId = userId,
                    Members = mdl.Members
                };
                var result = await _service.CreateCommunityAsync(community);
                
                
                return Ok(new ApiResponse { Code = 200, Message = "success", Data = result });
            }
            catch (Exception e)
            {
                return StatusCode(500, new ApiResponse { Code = 500, Message = e.Message, Data = null });
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpPost("leave")]
        public async Task<ActionResult<ApiResponse>> LeaveCommunity([FromQuery]string communityId)
        {
            try
            {

                var userId = GetUserId();

                var result = await _service.LeaveCommunityAsync(communityId, userId);

                return Ok(new ApiResponse { Code = 200, Message = "success", Data = result });
            }
            catch(Exception ex)
            {
                return BadRequest(new ApiResponse { Code = 400, Message = ex.Message, Data = null });
            }
        }


        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpPost("join")]
        public async Task<ActionResult<ApiResponse>> JoinCommunityUsingCode(string code)
        {
            try
            {

                var userId = GetUserId();

                var result = await _service.JoinCommunityUsingCodeAsync(code, userId);

                if (!result)
                {
                    return BadRequest(new ApiResponse { Code = 400, Message = "error joining the community", Data = null });
                }

                return Ok(new ApiResponse { Code = 200, Message = "success", Data = null });
            }
            catch (Exception e)
            {
                return StatusCode(500, new ApiResponse { Code = 500, Message = e.Message, Data = null });
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpPost("generate/{CommunityId}")]
        public async Task<ActionResult<ApiResponse>> GenerateInviteCode(string CommunityId)
        {
            try
            {
                var roles = GetUserRoles();
                var code = await _service.GenerateInviteCodeAsync(CommunityId, roles);

                if (code == null)
                {
                    return BadRequest(new ApiResponse
                    {
                        Code = 401,
                        Message = "User not authorized",
                        Data = null
                    });
                }

                return Ok(new ApiResponse
                {
                    Code = 200,
                    Message = "Invite code generated successfully",
                    Data = code
                });
            }
            catch (Exception e)
            {
                return StatusCode(500, new ApiResponse { Code = 500, Message = e.Message, Data = null });
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpDelete("delete")]
        public async Task<ActionResult<ApiResponse>> DeleteCommunity([FromQuery] string communityId)
        {
            try
            {
                var userId = GetUserId();
                
                var result = await _service.DeleteCommunityAsync(communityId, userId);
                if (!result)
                {
                    return BadRequest(new ApiResponse { Code = 400, Message = "error deleting the community", Data = null });
                }
                return Ok(new ApiResponse { Code = 200, Message = "success", Data = null });
            }
            catch (Exception e)
            {
                return StatusCode(500, new ApiResponse { Code = 500, Message = e.Message, Data = null });
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpGet]
        public async Task<ActionResult<ApiResponse>> GetAllCommunities()
        {
            try
            {
                var userId = GetUserId();
                var communities = await _service.GetAllCommuitiesAsync(userId);

                return Ok(new ApiResponse { Code = 200, Message = "success", Data = communities });
            }
            catch (Exception e)
            {
                return StatusCode(500, new ApiResponse { Code = 500, Message = e.Message, Data = null });
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpPut]
        public async Task<ActionResult<ApiResponse>> ModifyCommunity([FromQuery]string communityId, [FromBody]CommunityDtoToGet community)
        {
            try
            {
                
                var newCommunity = new Community
                {
                    CommunityId = communityId,
                    Name = community.Name,
                    Banner = community.Banner,
                    Description = community.Description,
                    IsDeleted = false,
                    IsPublic = community.IsPublic,
                    CreatorId = GetUserId(),
                    Members = community.Members
                };

                var result = await _service.ModifyCommunity(GetUserId(), communityId, newCommunity);
                if (result == null)
                {
                    return BadRequest(new ApiResponse
                    {
                        Code = 400,
                        Message = "Error modifying the community",
                        Data = null
                    });
                }
                
                return Ok(new ApiResponse
                {
                    Code = 200,
                    Message = "Community modified successfully",
                    Data = result
                });
            }
            catch (Exception e)
            {
                return StatusCode(500, new ApiResponse { Code = 500, Message = e.Message, Data = null });
            }
        }

        [HttpGet("users")]
        public async Task<ActionResult<ApiResponse>> GetUsers(string communityId)
        {

            try
            {
                var users = await _repo.GetAllUsers(communityId);
                if (users == null)
                {
                    return Ok(new ApiResponse
                    {
                        Code = 200,
                        Message = "No users found",
                        Data = null
                    });
                }
                return Ok(new ApiResponse
                {
                    Code = 200,
                    Message = "Users found",
                    Data = users
                });
            }
            catch (Exception e)
            {
                return StatusCode(500, new ApiResponse { Code = 500, Message = e.Message, Data = null });
            }
        }

        [HttpGet("public")]
        public async Task<ActionResult<Pagination<Community>>> GetPublicCommunities(int pageIndex, int pageSize, string? name)
        {
            try
            {
                var getPublicCommunitiesTask = _service.GetPublicCommunitiesAsync(pageIndex, pageSize, name);
                var getPublicCommunitiesCountTask = _repo.GetPublicCommunitiesCountAsync();

                await Task.WhenAll(getPublicCommunitiesTask, getPublicCommunitiesCountTask);

                var communities = await getPublicCommunitiesTask;

                return Ok(new ApiResponse { Code = 200, Message = "success", Data = new Pagination<Community>(pageIndex < 1 ? 1 : pageIndex, pageSize < 1 ? 5 : pageSize, await getPublicCommunitiesCountTask, communities) });
            }
            
            catch (Exception e)
            {
                return StatusCode(500, new ApiResponse { Code = 500, Message = e.Message, Data = null });
            }
        }




        private string GetUserId()
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            var userIdClaim = identity.FindFirst("user-id")!.ToString();
            var userId = userIdClaim.Split(':')[1].Trim();
            return userId;
        }

        private List<string> GetUserRoles()
        {
            var roles = User.Claims
            .Where(c => c.Type == ClaimTypes.Role || c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
            .Select(c => c.Value)
            .ToList();
            return roles;

        }

    }
}
