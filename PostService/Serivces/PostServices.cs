using Azure.Core;
using Microsoft.Extensions.Hosting;
using PostService.Core;
using PostService.Core.Interfaces;
using PostService.Dtos;
using PostService.Responses;
using System.Net.Http.Headers;
using System.Text.Json;
using Yam.Core.neo4j.Entities;
using Yam.Core.SharedServices;

namespace PostService.Serivces
{
    public class PostServices(IPostRepository repository, IHttpClientFactory httpClientFactory, IFileService fileService) : IPostService
    {
        
        private readonly IPostRepository _repository = repository;
        private readonly HttpClient _imageClient = httpClientFactory.CreateClient("ImageValidationService");
        private readonly IFileService _fileService = fileService;
        private readonly HttpClient _communityClient = httpClientFactory.CreateClient("CommunityValidationService");
        private readonly HttpClient _textClient = httpClientFactory.CreateClient("TextValidationService");


        public async Task<Post> CreatePostAsync(Post post, string communityId, string userId, AuthenticationHeaderValue token, IFormFile? imageFile) // 1. Make imageFile nullable
        {
            // It's better to manage the token on the client from the factory, but for now this is ok.
            _communityClient.DefaultRequestHeaders.Authorization = token;
            // --- Step 1: Prepare all potential async operations ---

            // Always validate user and text
            var communityValidationTask = _communityClient.GetAsync("");
            var textValidationTask = _textClient.PostAsJsonAsync("checktext", new { text = post.Content });

            // Conditionally prepare image tasks
            Task<HttpResponseMessage> imageValidationTask = null;
            Task<string> imageUploadTask = null; // This will hold the final URL

            if (imageFile != null && imageFile.Length > 0)
            {
                // Use a helper to avoid duplicating this code
                var imageContent = CreateImageContentForRequest(imageFile);
                imageValidationTask = _imageClient.PostAsync("checkimage", imageContent);
            }

            // --- Step 2: Run all required validations in parallel ---

            // Collect only the tasks that were actually started
            var validationTasks = new List<Task> { communityValidationTask, textValidationTask };
            if (imageValidationTask != null)
            {
                validationTasks.Add(imageValidationTask);
            }

            // Wait for all validation network calls to complete
            await Task.WhenAll(validationTasks);

            // --- Step 3: Process validation results and start the file upload ---

            // a) Process Community Validation
            
            
            var apiObject = await (await communityValidationTask).Content.ReadFromJsonAsync<ApiResponse<List<Community>>>(
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            if (apiObject?.Data == null || !apiObject.Data.Any(c => c.CommunityId == communityId))
            {
                throw new Exception("User is not authorized or community does not exist.");
            }

            // b) Process Text Validation
            await ValidateResponseAsync(
                await textValidationTask,
                (CheckTextResponseDto dto) => dto?.IsToxic == true,
                "Text validation service failed.",
                "The post content contains toxic language.");

            // c) Process Image Validation (only if it was run) and start the upload
            if (imageValidationTask != null)
            {
                await ValidateResponseAsync(
                    await imageValidationTask,
                    (CheckImageResponseDto dto) => dto?.Prediction?.IsHarmful == true,
                    "Image validation service failed.",
                    "The image contains harmful content.");

                // If validation passes, start the upload. We will await it later.
                imageUploadTask = _fileService.UploadAsync(imageFile);
            }

            // --- Step 4: Finalize and Save ---

            // If an image was uploaded, wait for the upload to finish and set the URL
            if (imageUploadTask != null)
            {
                post.ImageUrl = await imageUploadTask;
            }
            else
            {
                post.ImageUrl = "";
            }

            return await _repository.CreatePostAsync(post, communityId, userId);
        }

        public async Task<bool> DeletePostAsync(string postId, string userId)
        {
            var existingPost = await _repository.GetPostById(postId);
            if(existingPost is null || existingPost.CeatorId != userId)
            {
                return false;
            }

            var result = await _repository.DeletePostAsync(postId);
            return true;
        }

        public async Task<PaginatedResult<Post>>? GetAllPostsAsync(string communityId, string? sort, string? search, int pageNumber, int pageSize)
        {
            // Pass all parameters, including pagination, to the repository
            var posts = await _repository.GetAllPostsAsync(communityId, search, sort, pageNumber, pageSize);
            return posts;
        }

        public async Task<List<Post>>? GetPostsByUserIdAsync(string userId)
        {
            var post = await _repository.GetPostsByUserIdAsync(userId);
            return post;
        }

        public async Task<bool> LikeOrUnLikePostAsync(string postId, string userId)
        {
            var checkResult = await _repository.CheckUserLikeAsync(postId, userId);

            if(checkResult is null)
            {
                _repository.LikePostAsync(postId, userId);
                return true;
            }

            _repository.UnLikePostAsync(postId, userId);
            return false;
        }

       

        public async Task<Post> UpdatePostAsync(string postId, Post newPost, string userId, IFormFile imageFile)
        {
            try
            {
                var textValidationTask = _textClient.PostAsJsonAsync("checktext", new { text = newPost.Content });
                var existingPost = await _repository.GetPostById(postId);

                if (existingPost is null)
                    throw new Exception("Post doesn't exist");
                else if (existingPost.CeatorId != userId)
                    throw new Exception("User is not authorized");

                Task<HttpResponseMessage> imageValidationTask = null;
                Task<string> imageUploadTask = null; // This will hold the final URL

                if (imageFile != null && imageFile.Length > 0)
                {
                    // Use a helper to avoid duplicating this code
                    var imageContent = CreateImageContentForRequest(imageFile);
                    imageValidationTask = _imageClient.PostAsync("checkimage", imageContent);
                }

                // --- Step 2: Run all required validations in parallel ---

                // Collect only the tasks that were actually started
                var validationTasks = new List<Task> { textValidationTask };
                if (imageValidationTask != null)
                {
                    validationTasks.Add(imageValidationTask);
                }

                await Task.WhenAll(validationTasks);

                // --- Step 3: Process validation results and start the file upload 

                // b) Process Text Validation
                await ValidateResponseAsync(
                    await textValidationTask,
                    (CheckTextResponseDto dto) => dto?.IsToxic == true,
                    "Text validation service failed.",
                    "The post content contains toxic language.");

                // c) Process Image Validation (only if it was run) and start the upload
                if (imageValidationTask != null)
                {
                    await ValidateResponseAsync(
                        await imageValidationTask,
                        (CheckImageResponseDto dto) => dto?.Prediction?.IsHarmful == true,
                        "Image validation service failed.",
                        "The image contains harmful content.");

                    // If validation passes, start the upload. We will await it later.
                    imageUploadTask = _fileService.UploadAsync(imageFile);
                }

                // --- Step 4: Finalize and Save ---

                // If an image was uploaded, wait for the upload to finish and set the URL
                if (imageUploadTask != null)
                {
                    newPost.ImageUrl = await imageUploadTask;
                }
                else
                {
                    newPost.ImageUrl = "";
                }



                newPost.CreatedAt = existingPost.CreatedAt;
                newPost.CommentsCount = existingPost.CommentsCount;
                newPost.CommunityId = existingPost.CommunityId;
                newPost.LikesCount = existingPost.LikesCount;
                newPost.PostId = existingPost.PostId;

                var result = await _repository.UpdatePostAsync(postId, newPost);

                return result;
            }
            catch (Exception ex)
            {

                throw new Exception(ex.Message);
            }

        }



        // A private helper method to build the image content to avoid code duplication
        private MultipartFormDataContent CreateImageContentForRequest(IFormFile imageFile)
        {
            var multipartContent = new MultipartFormDataContent();
            var fileStream = imageFile.OpenReadStream();
            var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(imageFile.ContentType);
            multipartContent.Add(streamContent, "image", imageFile.FileName);
            return multipartContent;
        }

        // The generic validation helper method we created before
        private async Task ValidateResponseAsync<T>(
            HttpResponseMessage response,
            Func<T, bool> isInvalidCondition,
            string failureMessage,
            string invalidContentMessage) where T : class
        {
            if (!response.IsSuccessStatusCode) throw new Exception(failureMessage);

            var dto = await response.Content.ReadFromJsonAsync<T>();
            if (dto == null) throw new Exception("Failed to deserialize validation response.");

            if (isInvalidCondition(dto)) throw new Exception(invalidContentMessage);
        }
    }
}
