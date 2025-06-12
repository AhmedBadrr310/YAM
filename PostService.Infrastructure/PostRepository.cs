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
    public class PostRepository(IGraphClient graph) : IPostRepository
    {
        private readonly IGraphClient _graph = graph;

        public async  Task<Comment>? CreateCommentAsync(Comment comment, string postId, string userId)
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

        public async Task<Post>? CreatePostAsync(Post post, string communityId, string userId)
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
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<List<Post>>? GetAllPostsAsync(string communityId)
        {
            var results = await _graph.Cypher
                .Match("(p:Post)")
                .Where("p.CommunityId = $communityId")
                .WithParam("communityId", communityId)
                .Return(p => p.As<Post>())
                .ResultsAsync;
            if (results is null || !results.Any())
                return null;
            return results.ToList();
        }



        public async Task<List<Comment>>? GetCommentsByPostIdAsync(string postId)
        {
            return null;
        }

        public async Task<List<Post>>? GetPostsByUserIdAsync(string userId)
        {
            var results = await _graph.Cypher
                .Match("(u:User {UserId: $userId})-[:CREATED_BY]->(p:Post)")
                .WithParam("userId", userId)
                .Return(p => p.As<Post>())
                .ResultsAsync;
            if (results is null || !results.Any())
                return null;
            return results.ToList();
        }


        //under construction
        public async Task<bool> UpdatePostAsync(string postId, Post newPost)
        {
            try
            {
                await _graph.Cypher
                    .Match("(p:Post {PostId: $postId})")
                    .WithParam("postId", postId)
                    .Set("p = $newPost")
                    .WithParam("newPost", newPost)
                    .ExecuteWithoutResultsAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
    }
}
