using Neo4jClient;
using PostService.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yam.Core.neo4j.Entities;

namespace PostService.Infrastructure
{
    public class CommentRepository : ICommentRepository
    {
        private readonly IGraphClient _graphClient;

        public CommentRepository(IGraphClient graphClient)
        {
            _graphClient = graphClient;
        }

        public async Task<User> CheckForLikeAsync(string commentId, string userId)
        {
            var query = await _graphClient.Cypher
                .Match("(u:User)-[:LIKED]->(c:Comment)")
                .Where("u.UserId = $userId AND c.CommentId = $commentId")
                .WithParam("userId", userId)
                .WithParam("commentId", commentId)
                .Return(u => u.As<User>())
                .ResultsAsync;

            return query.FirstOrDefault();
        }

        public async Task<Comment> CreateCommentAsync(Comment comment, string postId, string userId)
        {
            try
            {
                var result = await _graphClient.Cypher
                        .Match("(p:Post)", "(u:User)")
                        .Where("p.PostId = $postId AND u.UserId = $userId")
                        .WithParam("postId", postId)
                        .WithParam("userId", userId)
                        .Create("(c:Comment)")
                        .Set("c = $comment")
                        .WithParam("comment", comment)
                        .Set("p.CommentsCount = coalesce(p.LikesCount, 0) + 1")
                        .Create("(c)-[:ASSOCIATED_WITH]->(p)")
                        .Create("(u)-[:COMMENTED]->(c)")
                        .Return(c => c.As<Comment>())
                        .ResultsAsync;

                return result.FirstOrDefault();
            }
            catch (Exception ex)
            {

                throw new Exception(ex.Message);
            }
        }

        public async Task<Comment> DeleteCommentAsync(string commentId, string userId)
        {
            var results = await _graphClient.Cypher
                .Match("(c:Comment)")
                .Where("c.CommentId = $commentId AND c.AuthorId = $userId")
                .WithParam("commentId", commentId)
                .WithParam("userId", userId)
                // Use DETACH DELETE to remove the node and its relationships
                .DetachDelete("c")
                .Return (c=>c.As<Comment>())
                .ResultsAsync; // Use ExecuteAsync to get stats
            
            _graphClient.Cypher
                .Match("(p.Post)")
                .Where("p.PostId = $postId")
                .WithParam("postId", results.FirstOrDefault().PostId)
                .Set("p.CommentsCount = coalesce(p.LikesCount, 0) - 1")
                .ExecuteWithoutResultsAsync();

            // Return true only if a node was actually deleted
            return results.FirstOrDefault();
        }

        public async Task<Comment> GetCommentByIdAsync(string commentId)
        {
            var query = await _graphClient.Cypher
                .Match("(c:Comment)")
                .Where("c.CommentId = $commentId")
                .WithParam("commentId", commentId)
                .Return(c => c.As<Comment>())
                .ResultsAsync;

            return query.FirstOrDefault();
        }

        public async Task<List<Comment>> GetCommentsByPostIdAsync(string postId, int pageIndex, int pageSize)
        {
            // Calculate the number of items to skip based on the page number and page size.
            var itemsToSkip = (pageIndex - 1) * pageSize;

            var query = await _graphClient.Cypher
                // Note: Make sure this relationship type is correct.
                .Match("(c:Comment)-[:ASSOCIATED_WITH]->(p:Post)")
                .Where("p.PostId = $postId")
                .WithParam("postId", postId)
                // 1. Order the comments to get a consistent result (e.g., newest first).
                .OrderByDescending("c.CreatedAt")
                // 2. Skip the items from previous pages.
                .Skip(itemsToSkip)
                // 3. Limit the number of items to the page size.
                .Limit(pageSize)
                .Return(c => c.As<Comment>())
                .ResultsAsync;

            return query.ToList();
        }

        public async Task<long> GetCountAsync(string postId)
        {
            var query = await _graphClient.Cypher
                .Match("(c:Comment)-[:ASSOCIATED_WITH]->(p:Post)")
                .Where("p.PostId = $postId")
                .WithParam("postId", postId)
                .Return(c => c.Count())
                .ResultsAsync;

            return query.FirstOrDefault();
        }

        public async Task LikeCommentAsync(string commentId, string userId)
        {
            await _graphClient.Cypher
                .Match("(u:User)", "(c:Comment)")
                .Where("u.UserId = $userId AND c.CommentId = $commentId")
                .WithParam("userId", userId)
                .WithParam("commentId", commentId)
                .Merge("(u)-[:LIKED]->(c)")
                // Add this SET clause to increment the count
                .Set("c.LikesCount = c.LikesCount + 1")
                .ExecuteWithoutResultsAsync();
        }

        public async Task UnLikeCommentAsync(string commentId, string userId)
        {
            await _graphClient.Cypher
                .Match("(u:User)-[r:LIKED]->(c:Comment)")
                .Where("u.UserId = $userId AND c.CommentId = $commentId")
                .WithParam("userId", userId)
                .WithParam("commentId", commentId)
                // Add this SET clause to decrement the count
                .Set("c.LikesCount = c.LikesCount - 1")
                .Delete("r")
                .ExecuteWithoutResultsAsync();
        }

        public async Task<Comment> UpdateCommentAsync(string commentId, Comment newComment, string userId)
        {
            // The newComment object should ideally be a DTO with only the editable fields.
            // Let's assume for now it only has a new 'Content'.

            var query = await _graphClient.Cypher
                .Match("(c:Comment)")
                .Where("c.CommentId = $commentId AND c.AuthorId = $userId")
                .WithParam("commentId", commentId)
                .WithParam("userId", userId)
                // Only set the properties that should be changed.
                .Set("c.Content = $newContent")
                .WithParam("newContent", newComment.Content)
                // Also a good idea to set an updated timestamp
                .Set("c.UpdatedAt = timestamp()")
                // Return the updated node from the database
                .Return(c => c.As<Comment>())
                .ResultsAsync;

            // Return the actual updated object from the DB, not the one passed in.
            return query.FirstOrDefault();
        }
    }
}
