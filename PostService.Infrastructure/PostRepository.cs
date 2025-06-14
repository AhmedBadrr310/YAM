using Microsoft.AspNetCore.Http.Timeouts;
using Neo4jClient;
using PostService.Core;
using PostService.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yam.Core.neo4j.Entities;

namespace PostService.Infrastructure
{
    public class PostRepository(IGraphClient graph) : IPostRepository
    {
        private readonly IGraphClient _graph = graph;

        public async Task<User> CheckUserLikeAsync(string postId, string userId)
        {
            var existingLike = await _graph.Cypher
                    .Match("(p:Post {PostId: $postId})<-[:LIKED]-(u:User {UserId: $userId})")
                    .WithParam("postId", postId)
                    .WithParam("userId", userId)
                    .Return(u => u.As<User>())
                    .ResultsAsync;

            return existingLike.FirstOrDefault();
        }

        public async  Task<Comment>? CreateCommentAsync(Comment comment, string postId, string userId)
        {
            try
            {
                var result = await _graph.Cypher
                         .Match("(p:Post)", "(u:User)")
                         .Where("p.PostId = $postId")
                         .WithParam("postId", postId)
                         .AndWhere("u.UserId = $userId")
                         .WithParam("userId", userId)
                         .Create("(c:Comment $commentParams)")
                         .WithParam("commentParams", comment)
                         .Create("(c)-[:COMMENT_ON]->(p)")
                         .Create("(c)-[:COMMENTED_BY]->(u)")
                         .Return(p => p.As<Comment>())
                         .ResultsAsync;
                return result.FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }

        public async Task<Post>? CreatePostAsync(Post post, string communityId, string userId)
        {

            try
            {
                var result = await _graph.Cypher
                      .Create("(p:Post $post)")
                      .WithParam("post", post)
                      .With("p")
                      .Match("(c:Community {CommunityId: $communityId})")
                      .WithParam("communityId", communityId)
                      .Create("(p)-[:BELONGS_TO]->(c)")
                      .With("p")
                      .Match("(u:User {UserId: $userId})")
                      .WithParam("userId", userId)
                      .Create("(p)-[:CREATED_BY]->(u)")
                      .Return(p => p.As<Post>())
                      .ResultsAsync;

                if (result == null || !result.Any())
                    return null;

                return result.FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<bool> DeletePostAsync(string postId)
        {
            try
            {
                await _graph.Cypher
                    .Match("(p:Post {PostId: $postId})")
                    .WithParam("postId", postId)
                    .DetachDelete("p")
                    .ExecuteWithoutResultsAsync();
                    
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<PaginatedResult<Post>>? GetAllPostsAsync(string communityId, string? searchValue, string? sort, int pageNumber, int pageSize)
        {
            try
            {
                // Base query to match and filter
                var query = _graph.Cypher
                    .Match("(p:Post)")
                    .Where("p.CommunityId = $communityId")
                    .WithParam("communityId", communityId);

                // Conditionally add search filter
                if (!string.IsNullOrEmpty(searchValue))
                {
                    query = query.AndWhere("toLower(p.Content) CONTAINS toLower($search)")
                        .WithParam("search", searchValue);
                }

                // --- New Logic for counting and pagination ---

                // This part gets the total count before sorting and paginating
                var countQuery = query
                    .Return(p => new { TotalCount = p.Count() });

                var countResult = await countQuery.ResultsAsync;
                var totalCount = (int)countResult.Single().TotalCount;

                if (totalCount == 0)
                {
                    return new PaginatedResult<Post>(new List<Post>(), 0);
                }

                // Calculate how many records to skip
                var recordsToSkip = (pageNumber - 1) * pageSize;

                // Add sorting, pagination, and return to the original query
                var dataQuery = query;

                if (sort == "asc")
                {
                    dataQuery = dataQuery.OrderBy("p.CreatedAt");
                }
                else
                {
                    // Default to descending if sort is null or not "asc"
                    dataQuery = dataQuery.OrderByDescending("p.CreatedAt");
                }

                var results = await dataQuery
                    .Skip(recordsToSkip)
                    .Limit(pageSize)
                    .Return(p => p.As<Post>())
                    .ResultsAsync;

                return new PaginatedResult<Post>(results.ToList(), totalCount);
            }
            catch (Exception ex)
            {
                // It's often better to log the exception and not expose raw messages
                throw new Exception("An error occurred while fetching posts.", ex);
            }
        }

        public async Task<Post>? GetPostById(string postId)
        {
            try
            {

                var post = await _graph.Cypher.Match("(p:Post)")
                                              .Where("p.PostId = $postId")
                                              .WithParam("postId", postId)
                                              .Return(p => p.As<Post>())
                                              .ResultsAsync;
                return post.FirstOrDefault();

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<List<Post>>? GetPostsByUserIdAsync(string userId)
        {
            try
            {
                var results = await _graph.Cypher
                        .Match("(p:Post)-[:CREATED_BY]->(:User {UserId: $userId})")
                        .WithParam("userId", userId)
                        .Return(p => p.As<Post>())
                        .ResultsAsync;
                if (results is null || !results.Any())
                    return null;
                return results.ToList();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task LikePostAsync(string postId, string userId)
        {
            try
            {
                
                // Create a relationship to indicate the user liked the post  
                await _graph.Cypher
                    .Match("(p:Post {PostId: $postId})", "(u:User {UserId: $userId})")
                    .WithParam("postId", postId)
                    .WithParam("userId", userId)
                    .Create("(u)-[:LIKED]->(p)")
                    .ExecuteWithoutResultsAsync();

                // Increment the likes count on the post  
                await _graph.Cypher
                    .Match("(p:Post {PostId: $postId})")
                    .WithParam("postId", postId)
                    .Set("p.LikesCount = coalesce(p.LikesCount, 0) + 1")
                    .ExecuteWithoutResultsAsync();

            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while liking the post.", ex);
            }
        }

        public async Task UnLikePostAsync(string postId, string userId)
        {
            try
            {
                // Remove the relationship indicating the user liked the post  
                await _graph.Cypher
                    .Match("(u:User {UserId: $userId})-[r:LIKED]->(p:Post {PostId: $postId})")
                    .WithParam("postId", postId)
                    .WithParam("userId", userId)
                    .Delete("r")
                    .ExecuteWithoutResultsAsync();

                // Decrement the likes count on the post  
                await _graph.Cypher
                    .Match("(p:Post {PostId: $postId})")
                    .WithParam("postId", postId)
                    .Set("p.LikesCount = coalesce(p.LikesCount, 0) - 1")
                    .ExecuteWithoutResultsAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while unliking the post.", ex);
            }
        }

        //under construction
        public async Task<Post> UpdatePostAsync(string postId, Post newPost)
        {
            try
            {
                var result = await _graph.Cypher
                    .Match("(p:Post {PostId: $postId})")
                    .WithParam("postId", postId)
                    .Set("p = $newPost")
                    .WithParam("newPost", newPost)
                    .Return(p => p.As<Post>())
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
