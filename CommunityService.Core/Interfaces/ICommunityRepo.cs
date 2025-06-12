using Yam.Core.neo4j.Entities;

namespace CommunityService.Core.Interfaces
{
    public interface ICommunityRepo
    {
        Task<Community> CreateCommunityAsync(Community community);

        Task<bool> DeleteCommunityAsync(string communityId);

        Task<bool> AddUserToCommunityAsync(string communityId, string userId);

        Task<List<Community>> GetAllCommuitiesAsync(string userId);

        Task<List<User>> GetAllUsers(string communityId);

        Task<User>? GetUser(string userId, string communityId);

        Task<Community> GetCommunityAsync(string communityId);

        Task<List<Community>>? GetPublicCommunities(int skip, int limit, string name);

        Task<long> GetPublicCommunitiesCountAsync();

        Task<Community> EditCommunity(Community community);

        Task<User>? GetAdmin(string communityId);

        Task<Community> GetCommunityByName(string commnuityName);

        Task<Community> RemoveUserFromCommunity(string communityId, string userId);
    }
}
