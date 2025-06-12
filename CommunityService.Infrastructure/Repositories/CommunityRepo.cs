using CommunityService.Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Neo4j.Driver;
using Neo4jClient;
using Yam.Core.neo4j.Entities;
using Yam.Core.neo4j.Relationships;
using Yam.Core.sql.Entities;

namespace CommunityService.Infrastructure.Repositories
{
    public class CommunityRepo(IGraphClient graphClient, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager) : ICommunityRepo
    {
        private readonly IGraphClient _graphClient = graphClient;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly RoleManager<ApplicationRole> _roleManager = roleManager;

        public async Task<bool> AddUserToCommunityAsync(string communityId, string userId)
        {
            try
            {

                var result = _graphClient.Cypher
                    .Match("(c:Community), (u:User)")
                    .Where((Community c) => c.CommunityId == communityId)
                    .AndWhere((User u) => u.UserId == userId)
                    .Create("(u)-[m:MEMBER $community ]->(c)")
                    .WithParam("community", new CommunityMembership
                    {
                        Status = "active",
                        MemberType = "member"
                    })
                    .Set("c.Members = c.Members + [u.UserId]")
                    .ExecuteWithoutResultsAsync();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<Community> CreateCommunityAsync(Community community)
        {
            try
            {
                var result = await _graphClient.Cypher
                   .Create("(c:Community $communityParams)")
                   .WithParam("communityParams", community)
                   .With("c")
                   .Match("(u:User)")
                   .Where("(u.UserId = $CreatorId)")
                   .WithParam("CreatorId", community.CreatorId)
                   .Create("(u)-[m:MEMBER $membership]->(c)")
                   .WithParam("membership", new CommunityMembership()
                   {
                       MemberType = "admin",
                       Status = "active"
                   })
                   .Return(c => c.As<Community>())
                   .ResultsAsync;

                return result.FirstOrDefault();
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                throw new Exception(ex.Message);
            }


        }

        public async Task<bool> DeleteCommunityAsync(string communityId)
        {


            try
            {
                // First, query to check if the node exists before deletion
                var nodeExists = await _graphClient.Cypher
                    .Match("(c:Community)")
                    .Where("(c.CommunityId = $comid)")
                    .WithParam("comid", communityId)
                    .Return(c => c.As<Community>())
                    .ResultsAsync;

                bool exists = nodeExists.Any();
                if (!exists)
                {
                    return false;
                }
                var tasks = new[]
                {
                    _graphClient.Cypher
                    .Match("(c:Community)")
                    .Where("(c.CommunityId = $comid)")
                    .WithParam("comid", communityId)
                    .DetachDelete("c")
                    .ExecuteWithoutResultsAsync(),
                    _roleManager.DeleteAsync(await _roleManager.FindByNameAsync(communityId))

                };
                // Then perform the deletion
                await Task.WhenAll(tasks);


                // Now you know if the node existed before deletion
                var wasSuccessful = exists;
                if (wasSuccessful && tasks[1].IsCompletedSuccessfully)
                {
                    // Optionally, you can log or return a success message
                    return true;
                }
                else
                {
                    // Optionally, you can log or return a failure message
                    return false;
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<List<Community>> GetAllCommuitiesAsync(string userId)
        {
            var query = _graphClient.Cypher
                .Match("(u:User)-[m:MEMBER]->(c:Community)")
                .Where((User u) => u.UserId == userId)
                .AndWhere("m.Status = 'active'")
                .Return(c =>
                     c.As<Community>()
                );

            var results = await query.ResultsAsync;

            return results.ToList();
        }

        public async Task<List<User>> GetAllUsers(string communityId)
        {
            var results = await _graphClient.Cypher
                .Match("(u:User)-[m:MEMBER]->(c:Community)")
                .Where((Community c) => c.CommunityId == communityId)
                .AndWhere("m.Status = 'active'")
                .OrderBy("u.Username")
                .Return(u => u.As<User>()).ResultsAsync;

            return results.ToList();
        }

        public async Task<Community> GetCommunityAsync(string communityId)
        {
            var result = await _graphClient.Cypher
                .Match("(c:Community)")
                .Where((Community c) => c.CommunityId == communityId)
                .Return(c => c.As<Community>())
                .ResultsAsync;

            return result.First();
        }

        public async Task<List<Community>>? GetPublicCommunities(int skip, int limit, string name)
        {

            IEnumerable<Community>? result;
            if (string.IsNullOrEmpty(name))
            {
                result = await _graphClient.Cypher
                    .Match("(c:Community)")
                    .Where("c.IsPublic = true")
                    .Skip(skip)
                    .Limit(limit)
                    .Return(c => c.As<Community>())
                    .ResultsAsync;
            }
            else
            {
                result = await _graphClient.Cypher
                    .Match("(c:Community)")
                    .Where("c.IsPublic = true")
                    .AndWhere("c.Name CONTAINS $name")
                    .WithParam("name", name)
                    .Skip(skip)
                    .Limit(limit)
                    .Return(c => c.As<Community>())
                    .ResultsAsync;
            }

            return result as List<Community>;
        }

        public async Task<long> GetPublicCommunitiesCountAsync()
        {
            var result = await _graphClient.Cypher
                .Match("(c:Community)")
                .Where("c.IsPublic = true")
                .Return(c=>c.Count())
                .ResultsAsync;
            return result.ElementAt(0);
        }

        public async Task<User>? GetUser(string userId, string communityId)
        {
            var result = await _graphClient.Cypher
                .Match("(u:User)-[m:MEMBER]->(c:Community)")
                .Where((User u) => u.UserId == userId)
                .AndWhere((Community c) => c.CommunityId == communityId)
                .AndWhere("m.Status = 'active'")
                .Return(u => u.As<User>())
                .ResultsAsync;

            if (!result.Any())
                return null!;
            return result.First();
        }

        public async Task<Community> EditCommunity(Community community)
        {
           var result = await _graphClient.Cypher
                .Match("(c:Community)")
                .Where((Community c) => c.CommunityId == community.CommunityId)
                .Set("c.Name = $name, c.Banner = $banner, c.Description = $description")
                .WithParam("name", community.Name)
                .WithParam("banner", community.Banner)
                .WithParam("description", community.Description)
                .Return(c => c.As<Community>())
                .ResultsAsync;
            return result.First();
        }

        public async Task<User>? GetAdmin(string communityId)
        {
            var result = await _graphClient.Cypher
                .Match("(u:User)-[m:MEMBER]->(c:Community)")
                .Where((Community c) => c.CommunityId == communityId)
                .AndWhere("m.MemberType = 'admin'")
                .Return(u => u.As<User>())
                .ResultsAsync;
            if (!result.Any())
                return null!;
            return result.First();
        }

        public async Task<Community> GetCommunityByName(string commnuityName)
        {
            var result = await _graphClient.Cypher
                .Match("(c:Community)")
                .Where("c.Name = $name")
                .WithParam("name", commnuityName)
                .Return(c => c.As<Community>())
                .ResultsAsync;
            return result.FirstOrDefault();
        }

        public async Task<Community> RemoveUserFromCommunity(string communityId, string userId)
        {
            try
            {
                // Remove the MEMBER relationship between the user and the community
                await _graphClient.Cypher
                    .Match("(u:User)-[m:MEMBER]->(c:Community)")
                    .Where((User u) => u.UserId == userId)
                    .AndWhere((Community c) => c.CommunityId == communityId)
                    .Delete("m")
                    .ExecuteWithoutResultsAsync();

                // Remove the userId from the Members list in the Community node
                var result = await _graphClient.Cypher
                    .Match("(c:Community)")
                    .Where((Community c) => c.CommunityId == communityId)
                    .Set("c.Members = [member IN c.Members WHERE member <> $userId]")
                    .WithParam("userId", userId)
                    .Return(c => c.As<Community>())
                    .ResultsAsync;

                return result.FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
