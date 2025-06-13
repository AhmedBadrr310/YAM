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
            // 1. Check for existing community name first and fail fast.
            var existingCommunity = await _repo.GetCommunityByName(community.Name);
            if (existingCommunity is not null)
            {
                throw new Exception("Community with this name already exists");
            }

            // 2. Prepare validation tasks without starting them.
            Task<HttpResponseMessage> imageValidationTask = null;
            Task<HttpResponseMessage> textValidationTask = null;

            if (banner is not null)
            {
                // Use a helper method to create the content to avoid duplicating code
                var imageContent = CreateImageContent(banner);
                imageValidationTask = _imageClient.PostAsync("checkimage", imageContent);
            }
            if (!string.IsNullOrEmpty(community.Name))
            {
                textValidationTask = _textClient.PostAsJsonAsync("checktext", new { text = community.Name });
            }

            // 3. Collect only the tasks that were actually started.
            var validationTasks = new List<Task>();
            if (imageValidationTask != null) validationTasks.Add(imageValidationTask);
            if (textValidationTask != null) validationTasks.Add(textValidationTask);

            // 4. Run all validation tasks in parallel.
            if (validationTasks.Any())
            {
                await Task.WhenAll(validationTasks);
            }

            // 5. Check results individually, only if the task was run.
            Task<string> uploadUrlTask = null;
            if (imageValidationTask != null)
            {
                await ValidateResponseAsync<CheckImageResponseDto>(
                    await imageValidationTask,
                    dto => dto?.Prediction?.IsHarmful == true,
                    "Image validation service failed.",
                    "The image contains sensitive content.");

                // If image validation passes, start the upload task.
                uploadUrlTask = _fileService.UploadAsync(banner);
            }
            if (textValidationTask != null)
            {
                await ValidateResponseAsync<CheckTextResponseDto>(
                    await textValidationTask,
                    dto => dto?.IsToxic == true,
                    "Text validation service failed.",
                    "The community name contains sensitive content.");
            }

            // --- The rest of your logic ---

            var role = new ApplicationRole()
            {
                CommunityId = community.CommunityId,
                Name = community.CommunityId
            };
            await _roleManager.CreateAsync(role);

            var user = await _userManager.FindByIdAsync(community.CreatorId);
            await _userManager.AddToRoleAsync(user, community.CommunityId);

            // This is a database operation, no need to run it in parallel with the upload.
            await _repo.AddUserToCommunityAsync(community.CommunityId, community.CreatorId);

            // Get the banner URL if the upload task was started
            if (uploadUrlTask != null)
            {
                community.BannerUrl = await uploadUrlTask;
            }

            return await _repo.CreateCommunityAsync(community);
        }

        // Helper method to avoid code duplication
        private MultipartFormDataContent CreateImageContent(IFormFile banner)
        {
            var multipartContent = new MultipartFormDataContent();
            var fileStream = banner.OpenReadStream();
            var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(banner.ContentType);
            multipartContent.Add(streamContent, "image", banner.FileName);
            return multipartContent;
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
            // 1. Authorization: Check if the user is the admin.
            var admin = await _repo.GetAdmin(communityId);
            if (admin is null || admin.UserId != userId)
            {
                throw new Exception("User is not authorized to modify this community.");
            }

            // You should probably also check if the new name is already taken by ANOTHER community.
            // This logic needs refinement, but for now we focus on the crash.

            // 2. Prepare validation tasks.
            Task<HttpResponseMessage> imageValidationTask = null;
            Task<HttpResponseMessage> textValidationTask = null;

            if (banner is not null)
            {
                var imageContent = CreateImageContent(banner); // Using the helper method from before
                imageValidationTask = _imageClient.PostAsync("checkimage", imageContent);
            }
            if (!string.IsNullOrEmpty(community.Name))
            {
                textValidationTask = _textClient.PostAsJsonAsync("checktext", new { text = community.Name });
            }

            // 3. Collect and run only the necessary tasks.
            var validationTasks = new List<Task>();
            if (imageValidationTask != null) validationTasks.Add(imageValidationTask);
            if (textValidationTask != null) validationTasks.Add(textValidationTask);

            if (validationTasks.Any())
            {
                await Task.WhenAll(validationTasks);
            }

            // 4. Check results individually and safely.
            Task<string> uploadUrlTask = null;
            if (imageValidationTask != null)
            {
                await ValidateResponseAsync<CheckImageResponseDto>(
                    await imageValidationTask,
                    dto => dto?.Prediction?.IsHarmful == true,
                    "Image validation service failed.",
                    "The image contains sensitive content.");

                // Start the upload task only after validation passes.
                uploadUrlTask = _fileService.UploadAsync(banner);
            }
            if (textValidationTask != null)
            {
                await ValidateResponseAsync<CheckTextResponseDto>(
                    await textValidationTask,
                    dto => dto?.IsToxic == true,
                    "Text validation service failed.",
                    "The community name contains sensitive content.");
            }

            // 5. Get the new banner URL if an image was uploaded.
            if (uploadUrlTask != null)
            {
                community.BannerUrl = await uploadUrlTask;
            }

            // 6. Save the modified community to the database.
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
