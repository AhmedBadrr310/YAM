using PostService.Core;
using PostService.Core.Interfaces;
using PostService.Dtos;
using PostService.Infrastructure;
using PostService.Responses;
using Yam.Core.neo4j.Entities;
using Yam.Core.SharedServices;

namespace PostService.Serivces
{
    public class CommentService(IFileService fileService, ICommentRepository commentRepository, IHttpClientFactory httpClientFactory, IPostRepository postRepository) : ICommentService
    {
        private readonly IFileService _fileService = fileService;
        private readonly ICommentRepository _commentRepository = commentRepository;
        private readonly IPostRepository _postRepository = postRepository;
        private readonly HttpClient _textClient = httpClientFactory.CreateClient("TextValidationService");
        private readonly HttpClient _communityClient = httpClientFactory.CreateClient("CommunityValidationService");

        public async Task<Comment> CreateCommentAsync(Comment comment, string postId, string userId)
        {
            try
            {
                // 1. Get the post to find out which community it belongs to.  
                var post = await _postRepository.GetPostById(postId);
                if (post == null)
                {
                    throw new Exception("Post not found.");
                }
                var communityId = post.CommunityId;

                // 2. Prepare all validation tasks to run in parallel.  
                var textValidationTask = _textClient.PostAsJsonAsync("checktext", new { text = comment.Content });

                // This assumes your CommunityService has an endpoint to check membership.  
                // You might need to add the user's auth token for this call.  
                var communityCheckTask = _communityClient.GetAsync($"users?communityId={communityId}");

                // 3. Run validations in parallel.  
                await Task.WhenAll(textValidationTask, communityCheckTask);

                // 4. Process the results.  
                // a) Validate text  
                await ValidateResponseAsync<CheckTextResponseDto>(
                    await textValidationTask,
                    dto => dto?.IsToxic == true,
                    "Text validation service failed.",
                    "The comment contains toxic content.");

                // b) Validate community membership  
                var stringResponse = await communityCheckTask.Result.Content.ReadAsStringAsync();
                var communityCheckResponse = await communityCheckTask.Result.Content.ReadFromJsonAsync<ApiResponse<List<UserDtoToGet>>>();
                if (communityCheckResponse?.Data == null)
                {
                    throw new Exception("Community validation service returned null data.");
                }
                bool isMember = false;
                foreach (var user in communityCheckResponse.Data)
                {
                    if (user.UserId == userId)
                    {
                        isMember = true;
                        break;
                    }
                }
                if (!isMember)
                {
                    throw new Exception("User is not a member of the community.");
                }
                // 5. If all validations pass, set properties and create the comment.  
                comment.AuthorId = userId;
                comment.PostId = postId;
                return await _commentRepository.CreateCommentAsync(comment, postId, userId);
            }
            catch (Exception ex)
            {

                throw new Exception(ex.Message);
            }
        }

        public async Task<bool> DeleteCommentAsync(string commentId, string userId)
        {
            var result = await _commentRepository.DeleteCommentAsync(commentId, userId);

            if (result is null)
                throw new Exception("User is not authorized");

            return true;
        }

        public async Task<PaginatedResult<Comment>> GetCommentsByPostIdAsync(string postId, int pageIndex, int pageSize)
        {
            var commentsTask = _commentRepository.GetCommentsByPostIdAsync(postId, pageIndex, pageSize);
            var countTask = _commentRepository.GetCountAsync(postId);

            Task.WhenAll(commentsTask, countTask);

            return new PaginatedResult<Comment>(await commentsTask, await countTask);
        }

        public async Task<bool> LikeOrUnLikeCommentAsync(string commentId, string userId)
        {
            var checkResult = await _commentRepository.CheckForLikeAsync(commentId, userId);

            if (checkResult is null)
            {
                _commentRepository.LikeCommentAsync(commentId, userId);
                return true;
            }

            _commentRepository.UnLikeCommentAsync(commentId, userId);
            return false;
        }

        public Task<Comment> UpdateCommentAsync(string commentId, Comment newComment, string userId)
        {
            throw new NotImplementedException();
        }


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
