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

        public PostController(
            IHubContext<PostHub> hub,
            IHttpContextAccessor http,
            IUserService userService)
        {
            _hub = hub;
            _http = http;
            _userService = userService;
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

                // 5. Broadcast via SignalR (To the Page/User context only)
                string targetGroup = GetSignalRGroupName(req.ContextType, req.ContextId);
                await _hub.Clients.Group(targetGroup).SendAsync("ReceivePost", new
                {
                    targetGroup = targetGroup,
                    data = livePost
                });

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

                // ALGORITHM: RANDOM DISCOVERY
                // If the client asks for "global", we ignore context ID and fetch random posts.
                if (contextType == "global")
                {
                    var randomPosts = io.GetDiscoveryFeed(viewerId);
                    return Ok(randomPosts);
                }

                // STANDARD FETCH (User Page, Location Page, etc.)
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
        public IActionResult AddComment([FromBody] CreateCommentDto req)
        {
            try
            {
                int userId = GetUserId();
                var io = new InsertComment();
                long id = io.Execute(userId, req);
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
        public IActionResult LikePost(long id)
        {
            try
            {
                int userId = GetUserId();
                var io = new ToggleLike();
                bool liked = io.Execute(userId, id);
                return Ok(new { liked });
            }
            catch (UnauthorizedAccessException) { return Unauthorized(); }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }
    }
}