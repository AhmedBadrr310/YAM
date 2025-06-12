using CommunityService.Core.Interfaces;
using CommunityService.Respones;
using Microsoft.AspNetCore.Identity;
using StackExchange.Redis;
using Yam.Core.neo4j.Entities;
using Yam.Core.sql.Entities;

namespace CommunityService.Services
{
    public class CommunityServices(ICommunityRepo repo, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager, IConnectionMultiplexer multiplexer) : ICommunityService
    {
        private readonly ICommunityRepo _repo = repo;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly RoleManager<ApplicationRole> _roleManager = roleManager;
        private readonly IDatabase _redisDb = multiplexer.GetDatabase();

        public async Task<bool> AddUserToCommunityAsync(string communityId, string userId)
        {
            // Retrieve the community from the repository  
            var user = await _repo.GetUser(userId, communityId);
            if (user is not null)
            {
                return false;
            }

            // Update the community in the repository  
            return await _repo.AddUserToCommunityAsync(communityId, userId);
        }

        public async Task<Community> CreateCommunityAsync(Community community)
        {
            var result = await _repo.GetCommunityByName(community.Name);
            if(result is not null)
            {
                throw new Exception("Community with this name already exists");
            }
            var role = new ApplicationRole()
            {
                CommunityId = community.CommunityId,
                Name = community.CommunityId
            };
            await _roleManager.CreateAsync(role);
            var user = await _userManager.FindByIdAsync(community.CreatorId);
            await _userManager.AddToRoleAsync(user, community.CommunityId);
            return await _repo.CreateCommunityAsync(community);

        }

        public async Task<bool> DeleteCommunityAsync(string communityId, string userId)
        {
            try
            {
                var admin = await _repo.GetAdmin(communityId);
                if(admin is null || admin.UserId != userId)
                {
                    return false;
                }

                var result = await _repo.DeleteCommunityAsync(communityId);
                if (result)
                {
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<string>? GenerateInviteCodeAsync(string communityId, List<string> roles)
        {
            bool isAdmin = false;
            foreach (var role in roles)
            {
                isAdmin = role == communityId ? true: false;
                if (isAdmin)
                {
                    break;
                }
            }
            
            if(!isAdmin)
            {
                return null;
            }

            var code = Guid.NewGuid().ToString().Substring(0,6);
            while (true)
            {
                var exists = await _redisDb.StringGetAsync(code);
                if (!exists.IsNullOrEmpty)
                {
                    code = Guid.NewGuid().ToString().Substring(0, 6);
                    continue;
                }
                break;
            }
            
            _redisDb.StringSetAsync(code, communityId, TimeSpan.FromDays(1));

            return code;
        }

        public Task<List<Community>>? GetAllCommuitiesAsync(string userId)
        {
            return _repo.GetAllCommuitiesAsync(userId);
        }

        public Task<List<User>> GetAllUsers(string communityId)
        {
            return _repo.GetAllUsers(communityId);
        }

        public async Task<List<Community>> GetPublicCommunitiesAsync(int pageIndex, int pageSize, string? name)
        {
            if(pageIndex < 1 )
                pageIndex = 1;
            if(pageSize < 1)
                pageSize = 10;
            
            
            var result = await _repo.GetPublicCommunities((pageIndex-1)*pageSize, pageSize, name);
            return result;
        }

        public async Task<bool> JoinCommunityUsingCodeAsync(string code, string userId)
        {
            var result = await _redisDb.StringGetAsync(code);

            if (result.IsNullOrEmpty)
            {
                return false;
            }

            var existUser = await _repo.GetUser(result.ToString(), userId);
            if(existUser is not null)
            {
                return false;
            }

            var addingResult = await AddUserToCommunityAsync(result.ToString(), userId);
            if(!addingResult)
            {
                return false;
            }

            return true;

        }

        public async Task<Community> LeaveCommunityAsync(string communityId, string userId)
        {
            var communityUsers = await  _repo.GetAllUsers(communityId);
            bool userExist = false;
            foreach(var communityUser in communityUsers)
            {
                if(communityUser.UserId == userId)
                {
                    userExist = true;
                    break;
                }
            }

            if (!userExist)
            {
                throw new Exception("user doesnt exist in the community");
            }

            var adminUser = await _repo.GetAdmin(communityId);
            if(adminUser.UserId == userId)
            {
                throw new Exception("the Admin cannot leave the community");
            }

            var result = await _repo.RemoveUserFromCommunity(communityId, userId);
            if(result is null)
            {
                throw new Exception("error leaving the community");
            }

            return result;
        }

        public async Task<Community> ModifyCommunity(string userId, string communityId, Community community)
        {
            var admin = await _repo.GetAdmin(communityId);
            if(admin is null || admin.UserId != userId)
            {
                return null;
            }
               
            var result = await _repo.EditCommunity(community);
            return community;
        }
    }
}
