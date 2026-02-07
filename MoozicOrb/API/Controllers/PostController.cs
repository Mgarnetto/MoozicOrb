using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MoozicOrb.API.Models;
using MoozicOrb.Hubs;
using MoozicOrb.IO;
using MoozicOrb.Services;
using MoozicOrb.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MoozicOrb.API.Controllers
{
    [ApiController]
    [Route("api/posts")]
    public class PostController : ControllerBase
    {
        private readonly IHubContext<PostHub> _hub;
        private readonly IHttpContextAccessor _http;
        private readonly IUserService _userService;
        private readonly NotificationService _notify; // <--- ADDED

        public PostController(
            IHubContext<PostHub> hub,
            IHttpContextAccessor http,
            IUserService userService,
            NotificationService notify) // <--- INJECTED
        {
            _hub = hub;
            _http = http;
            _userService = userService;
            _notify = notify;
        }

        // --- HELPERS ----------------------------------------------------

        private int GetUserId()
        {
            var sid = _http.HttpContext?.Request.Headers["X-Session-Id"].ToString();
            if (string.IsNullOrEmpty(sid)) throw new UnauthorizedAccessException();
            var session = SessionStore.GetSession(sid);
            if (session == null) throw new UnauthorizedAccessException();
            return session.UserId;
        }

        private int GetViewerId()
        {
            try { return GetUserId(); }
            catch { return 0; }
        }

        private string GetSignalRGroupName(string type, string id)
        {
            return (type?.ToLower()) switch
            {
                "loc" => $"loc_{id}",
                "page" => $"page_{id}",
                "user" => $"user_{id}",
                "creator" => $"user_{id}",
                "feed" => "feed_global",
                "discover" => "page_discover",
                _ => "feed_global"
            };
        }

        // --- POSTS ------------------------------------------------------

        [HttpPost]
        public async Task<IActionResult> CreatePost([FromBody] CreatePostDto req)
        {
            try
            {
                int userId = GetUserId();

                // 1. Fetch Real User Info for the live update
                var user = new UserQuery().GetUserById(userId);
                string authorName = user?.UserName ?? "Unknown";
                string authorPic = user?.ProfilePic ?? "/img/profile_default.jpg";

                // 2. Insert Post
                var postIo = new InsertPost();
                long postId = postIo.Execute(userId, req);

                // 3. Insert Attachments
                if (req.MediaAttachments != null && req.MediaAttachments.Count > 0)
                {
                    var mediaIo = new InsertPostMedia();
                    int sort = 0;
                    foreach (var item in req.MediaAttachments)
                    {
                        mediaIo.Execute(postId, item.MediaId, item.MediaType, sort++);
                    }
                }

                // 4. Construct DTO for Broadcast
                var livePost = new PostDto
                {
                    Id = postId,
                    AuthorId = userId,
                    AuthorName = authorName,
                    AuthorPic = authorPic,
                    ContextType = req.ContextType,
                    ContextId = req.ContextId,
                    Type = req.Type,
                    Title = req.Title,
                    Text = req.Text,
                    ImageUrl = req.ImageUrl,
                    CreatedAt = DateTime.UtcNow,
                    CreatedAgo = "Just now",
                    Attachments = req.MediaAttachments ?? new List<MediaAttachmentDto>(),
                    Price = req.Price,
                    LocationLabel = req.LocationLabel,
                    DifficultyLevel = req.DifficultyLevel,
                    VideoUrl = req.VideoUrl,
                    MediaId = req.MediaId,
                    Category = req.Category,
                    IsLiked = false,
                    LikesCount = 0,
                    CommentsCount = 0
                };

                // 5. Broadcast via SignalR
                string targetGroup = GetSignalRGroupName(req.ContextType, req.ContextId);
                await _hub.Clients.Group(targetGroup).SendAsync("ReceivePost", new
                {
                    targetGroup = targetGroup,
                    data = livePost
                });

                // 6. NOTIFY FOLLOWERS (NEW)
                string preview = req.Title ?? (req.Text?.Length > 20 ? req.Text.Substring(0, 20) + "..." : "New Post");
                // Fire and forget (or await if you prefer safety)
                await _notify.NotifyFollowers(userId, postId, preview);

                return Ok(new { id = postId });
            }
            catch (UnauthorizedAccessException) { return Unauthorized(); }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpGet]
        public IActionResult GetPosts(
            [FromQuery] string contextType,
            [FromQuery] string contextId,
            [FromQuery] int page = 1)
        {
            try
            {
                int viewerId = GetViewerId();
                var io = new GetPost();

                if (contextType == "global" || contextType == "feed_global")
                {
                    var randomPosts = io.GetDiscoveryFeed(viewerId);
                    return Ok(randomPosts);
                }

                if (contextType == "discover")
                {
                    var audioPosts = io.GetAudioDiscoveryFeed(viewerId);
                    return Ok(audioPosts);
                }

                var posts = io.Execute(contextType, contextId, viewerId, page);
                return Ok(posts);
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpGet("{id}")]
        public IActionResult GetSingle(long id)
        {
            try
            {
                int viewerId = GetViewerId();
                var io = new GetPost();
                var post = io.Execute(id, viewerId);

                if (post == null) return NotFound("Post not found");
                return Ok(post);
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        // --- COMMENTS ---------------------------------------------------

        [HttpPost("comment")]
        public async Task<IActionResult> AddComment([FromBody] CreateCommentDto req) // Changed to async Task
        {
            try
            {
                int userId = GetUserId();
                var io = new InsertComment();
                long id = io.Execute(userId, req);

                // NOTIFY POST AUTHOR (NEW)
                var postIo = new GetPost();
                var post = postIo.Execute(req.PostId, userId);

                // Don't notify if commenting on own post
                if (post != null && post.AuthorId != userId)
                {
                    string commentPreview = req.Content.Length > 20 ? req.Content.Substring(0, 20) + "..." : req.Content;
                    await _notify.NotifyUser(post.AuthorId, userId, "comment", req.PostId, $"commented: {commentPreview}");
                }

                return Ok(new { id });
            }
            catch (UnauthorizedAccessException) { return Unauthorized(); }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpGet("{id}/comments")]
        public IActionResult GetComments(long id)
        {
            try
            {
                var io = new GetComments();
                var comments = io.Execute(id);
                return Ok(comments);
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        // --- LIKES ------------------------------------------------------

        [HttpPost("{id}/like")]
        public async Task<IActionResult> LikePost(long id) // Changed to async Task
        {
            try
            {
                int userId = GetUserId();
                var io = new ToggleLike();
                bool liked = io.Execute(userId, id);

                // NOTIFY POST AUTHOR (NEW)
                if (liked)
                {
                    var postIo = new GetPost();
                    var post = postIo.Execute(id, userId);

                    // Don't notify if liking own post
                    if (post != null && post.AuthorId != userId)
                    {
                        await _notify.NotifyUser(post.AuthorId, userId, "like", id, "liked your post");
                    }
                }

                return Ok(new { liked });
            }
            catch (UnauthorizedAccessException) { return Unauthorized(); }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        // --- UPDATE / DELETE --------------------------------------------

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePost(long id, [FromBody] UpdatePostDto req)
        {
            try
            {
                int userId = GetUserId();
                var io = new UpdatePost();
                io.Execute(userId, id, req);

                var getIo = new GetPost();
                var updatedPost = getIo.Execute(id, userId);

                if (updatedPost != null)
                {
                    string targetGroup = GetSignalRGroupName(updatedPost.ContextType, updatedPost.ContextId);
                    await _hub.Clients.Group(targetGroup).SendAsync("UpdatePost", new
                    {
                        postId = id,
                        data = updatedPost
                    });
                }

                return Ok(new { success = true });
            }
            catch (UnauthorizedAccessException) { return Unauthorized(); }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePost(long id)
        {
            try
            {
                int userId = GetUserId();
                var getIo = new GetPost();
                var existing = getIo.Execute(id, userId);
                if (existing == null) return NotFound();

                var delIo = new DeletePost();
                bool success = delIo.Execute(userId, id);

                if (success)
                {
                    string targetGroup = GetSignalRGroupName(existing.ContextType, existing.ContextId);
                    await _hub.Clients.Group(targetGroup).SendAsync("RemovePost", new { postId = id });
                    return Ok(new { success = true });
                }
                return BadRequest("Could not delete post.");
            }
            catch (UnauthorizedAccessException) { return Unauthorized(); }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpDelete("{id}/media/{mediaId}")]
        public IActionResult DeleteMedia(long id, long mediaId)
        {
            try
            {
                int userId = GetUserId();
                var io = new DeletePostMedia();
                io.Execute(userId, id, mediaId);
                return Ok(new { success = true });
            }
            catch (UnauthorizedAccessException) { return Unauthorized(); }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }
    }
}