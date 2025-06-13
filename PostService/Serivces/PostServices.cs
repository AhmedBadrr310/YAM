using Azure.Core;
using PostService.Core.Interfaces;
using PostService.Dtos;
using PostService.Responses;
using System.Net.Http.Headers;
using System.Text.Json;
using Yam.Core.neo4j.Entities;
using Yam.Core.SharedServices;

namespace PostService.Serivces
{
    public class PostServices(IPostRepository repository, HttpClient httpClient, IFileService fileService) : IPostService
    {
        
        private readonly IPostRepository _repository = repository;
        private readonly HttpClient _httpClient = httpClient;
        private readonly IFileService _fileService = fileService;
        private readonly HttpClient _client = new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true });
        private readonly string CommunitySerivceUrl = "https://20.215.192.90:5002/api/Commuity";
        private readonly HttpClient _textClient = new HttpClient();


        public async Task<Post>? CreatePostAsync(Post post, string communityId, string userId, AuthenticationHeaderValue token, IFormFile imageFile)
        {
            
            _client.DefaultRequestHeaders.Authorization = token;
            using var multipartContent = new MultipartFormDataContent();

            // Get the stream directly from the incoming IFormFile
            await using var fileStream = imageFile.OpenReadStream();
            var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(imageFile.ContentType);

            // --- THIS IS THE KEY PART ---
            // Add the file stream to the new request.
            // The name "image" matches the "Key" in your Postman screenshot.
            multipartContent.Add(streamContent, "image", imageFile.FileName);
            var tasks = new[]
            {
                _client.GetAsync(CommunitySerivceUrl),
                _httpClient.PostAsync("checkimage", multipartContent),
                _textClient.PostAsJsonAsync("http://20.215.192.90:5000/checktext", new{text=post.Content})
            };

            var responses = await Task.WhenAll(tasks);

            //Converting the response to a string
            #region UserValidation
            var stringResponseForCommunities = await responses[0].Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var apiObject = JsonSerializer.Deserialize<ApiResponse<List<Community>>>(stringResponseForCommunities, options);

            List<Community> communities = apiObject.Data;
            if (communities == null || !communities.Any() || !communities.Any(c => c.CommunityId == communityId))
            {
                throw new Exception("User is not authorized");
            }
            #endregion

            #region ImageValidation
            if (!responses[1].IsSuccessStatusCode)
            {
                throw new Exception("Image validation failed.");
            }

            var responseForImage = await responses[1].Content.ReadFromJsonAsync<CheckImageResponseDto>();

            if (responseForImage.Prediction.IsHarmful)
            {
                throw new Exception("The image contains harmful content.");
            }
            #endregion

            #region TextValidation
            var responseForTextDto = await responses[2].Content.ReadFromJsonAsync<CheckTextResponseDto>();


            if (responseForTextDto.IsToxic)
            {
                throw new Exception("The text contains toxic content.");
            }
            #endregion

          
            var imageUrl = await _fileService.UploadAsync(imageFile);
            
            post.ImageUrl = imageUrl;


            return await _repository.CreatePostAsync(post, communityId, userId);

        }

        public Task<bool> DeletePostAsync(string postId)
        {
            throw new NotImplementedException();
        }

        public Task<List<Post>>? GetAllPostsAsync(string communityId)
        {
            throw new NotImplementedException();
        }

        public Task<List<Post>>? GetPostsByUserIdAsync(string userId)
        {
            throw new NotImplementedException();
        }

        public Task<Post> UpdatePostAsync(string postId, Post newPost)
        {
            throw new NotImplementedException();
        }
    }
}
