using CommunityService.Core.Interfaces;
using CommunityService.Dtos;
using CommunityService.Respones;
using Microsoft.AspNetCore.Identity;
using StackExchange.Redis;
using System.Net.Http.Headers;
using Yam.Core.neo4j.Entities;
using Yam.Core.SharedServices;
using Yam.Core.sql.Entities;

namespace CommunityService.Services
{
    public class CommunityServices(ICommunityRepo repo, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager, IConnectionMultiplexer multiplexer, IFileService fileService, IHttpClientFactory httpClientFactory) : ICommunityService
    {
        private readonly ICommunityRepo _repo = repo;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly RoleManager<ApplicationRole> _roleManager = roleManager;
        private readonly IFileService _fileService = fileService;
        private readonly HttpClient _textClient = httpClientFactory.CreateClient("TextValidationService");
        private readonly HttpClient _imageClient = httpClientFactory.CreateClient("ImageValidationService");
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

        public async Task<Community> CreateCommunityAsync(Community community, IFormFile banner)
        {
            var resultAsync = _repo.GetCommunityByName(community.Name);
            var tasksToRun = new List<Task<HttpResponseMessage>>(); // Explicitly specify the type of tasks in the list.  
            bool validatingImage = false;
            if (banner is not null)
            {
                using var multipartContent = new MultipartFormDataContent();
                await using var fileStream = banner.OpenReadStream();
                var streamContent = new StreamContent(fileStream);
                multipartContent.Add(streamContent, "image", banner.FileName);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(banner.ContentType);

                tasksToRun.Add(_imageClient.PostAsync("checkimage", multipartContent));
                validatingImage = true;
            }
            if (!string.IsNullOrEmpty(community.Name))
            {
                tasksToRun.Add(_textClient.PostAsJsonAsync("checktext", new { text = community.Name }));
            }


            var result = await resultAsync;
            if(result is not null)
            {
                throw new Exception("Community with this name already exists");
            }


            Task<string> urlTask = null;
            if (tasksToRun.Any())
            {
                var responses = await Task.WhenAll(tasksToRun); // Ensure the type matches the expected return type of Task.WhenAll.  
                if (validatingImage)
                {
                    urlTask = _fileService.UploadAsync(banner);
                    await ValidateResponseAsync<CheckImageResponseDto>(responses[0], dto => dto.Prediction.IsHarmful, "failed to check the image", "the image has sensitive content");

                }
                #region ImageValidation  
                #endregion

                #region TextValidation  
                await ValidateResponseAsync<CheckTextResponseDto>(responses[validatingImage?1:0], dto => dto.IsToxic, "failed to check the name", "the text has sensitive content");
                #endregion
            }


            var role = new ApplicationRole()
            {
                CommunityId = community.CommunityId,
                Name = community.CommunityId
            };
            await _roleManager.CreateAsync(role);
            var userTask = _userManager.FindByIdAsync(community.CreatorId);



            var addRoleTask = _repo.AddUserToCommunityAsync(community.CommunityId, community.CreatorId);
            if (urlTask is not null)
                await Task.WhenAll(urlTask, addRoleTask, userTask);
            else
                await Task.WhenAll( addRoleTask, userTask);
            await _userManager.AddToRoleAsync(await userTask, community.CommunityId);
            if (urlTask is not null)
            {
                community.BannerUrl = await urlTask;  
            }
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

        public async Task<string>? GenerateInviteCodeAsync(string communityId, string userId)
        {

            var admin = await _repo.GetAdmin(communityId);
            bool isAdmin = admin.UserId == userId ? true : false;
            
            if(!isAdmin)
            {
                throw new Exception("User is not authorized");
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

        public async Task<Community> ModifyCommunity(string userId, string communityId, Community community, IFormFile banner)
        {
            
            var resultAsync = _repo.GetCommunityByName(community.Name);
            var tasksToRun = new List<Task<HttpResponseMessage>>(); // Explicitly specify the type of tasks in the list.  
            bool validatingImage = false;
            if (banner is not null)
            {
                using var multipartContent = new MultipartFormDataContent();
                await using var fileStream = banner.OpenReadStream();
                var streamContent = new StreamContent(fileStream);
                multipartContent.Add(streamContent, "image", banner.FileName);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(banner.ContentType);

                tasksToRun.Add(_imageClient.PostAsync("checkimage", multipartContent));
                validatingImage = true;
            }
            if (!string.IsNullOrEmpty(community.Name))
            {
                tasksToRun.Add(_textClient.PostAsJsonAsync("checktext", new { text = community.Name }));
            }

            var admin = await _repo.GetAdmin(communityId);
            if (admin is null || admin.UserId != userId)
            {
                throw new Exception("user is not authorized");
            }

            Task<string> urlTask = null;
            if (tasksToRun.Any())
            {
                var responses = await Task.WhenAll(tasksToRun); // Ensure the type matches the expected return type of Task.WhenAll.  
                if(validatingImage)
                {
                    urlTask = _fileService.UploadAsync(banner);
                    await ValidateResponseAsync<CheckImageResponseDto>(responses[0], dto => dto.Prediction.IsHarmful, "failed to check the image", "the image has sensitive content");

                }
                #region ImageValidation  
                #endregion

                #region TextValidation  
                await ValidateResponseAsync<CheckTextResponseDto>(responses[validatingImage?1:0], dto => dto.IsToxic, "failed to check the name", "the text has sensitive content");
                #endregion
            }

            if(urlTask is not null)
            {
                var url = await urlTask;
                community.BannerUrl = url;
            }

            var result = await _repo.EditCommunity(community);
            return result;
        }


        private async Task ValidateResponseAsync<T>(
            HttpResponseMessage response,
            Func<T, bool> isInvalidCondition,
            string failureMessage,
            string invalidContentMessage)
        {
            // 1. Check if the HTTP call was successful.
            if (!response.IsSuccessStatusCode)
            {
                // This makes your text validation more robust by adding the missing check.
                throw new Exception(failureMessage);
            }

            // 2. Deserialize the JSON content to the specified DTO type.
            var dto = await response.Content.ReadFromJsonAsync<T>();

            // It's good practice to check if deserialization was successful.
            if (dto == null)
            {
                throw new Exception("Failed to deserialize validation response.");
            }

            // 3. Use the provided lambda function to check the business logic rule.
            if (isInvalidCondition(dto))
            {
                throw new Exception(invalidContentMessage);
            }
        }
    }
}
