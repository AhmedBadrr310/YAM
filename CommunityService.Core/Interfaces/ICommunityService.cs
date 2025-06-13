using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yam.AuthService.Responses;
using Yam.Core.neo4j.Entities;

namespace CommunityService.Core.Interfaces
{
    public interface ICommunityService
    {
        Task<Community> CreateCommunityAsync(Community community, IFormFile banner);

        Task<bool> DeleteCommunityAsync(string communityId, string userId);

        Task<bool> AddUserToCommunityAsync(string communityId, string userId);

        Task<bool> JoinCommunityUsingCodeAsync(string code, string userId);

        Task<List<Community>>? GetAllCommuitiesAsync(string userId);

        Task<List<User>> GetAllUsers(string communityId);

        Task<List<Community>> GetPublicCommunitiesAsync(int pageIndex, int pageSize, string? name);

        Task<Community> ModifyCommunity(string userId, string communityId, Community community, IFormFile banner);

        Task<string>? GenerateInviteCodeAsync(string communityId, string userId);

        Task<Community> LeaveCommunityAsync(string communityId, string userId);
    }
}
