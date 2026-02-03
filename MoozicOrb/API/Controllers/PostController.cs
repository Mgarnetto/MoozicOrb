using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MoozicOrb.API.Models;
using MoozicOrb.Hubs;
using MoozicOrb.IO;
using MoozicOrb.Services;
using MoozicOrb.Services.Interfaces; // [Added] For IUserService
using System;
using System.Threading.Tasks;

namespace MoozicOrb.API.Controllers
{
    [ApiController]
    [Route("api/posts")]
    public class PostController : ControllerBase
    {
        private readonly IHubContext<PostHub> _hub;
        private readonly IHttpContextAccessor _http;
        private readonly IUserService _userService; // [Added]

        // [Updated Constructor]
        public PostController(
            IHubContext<PostHub> hub,
            IHttpContextAccessor http,
            IUserService userService)
        {
            _hub = hub;
            _http = http;
            _userService = userService;
        }

        private int GetUserId()
        {
            var sid = _http.HttpContext?.Request.Headers["X-Session-Id"].ToString();
            if (string.IsNullOrEmpty(sid)) throw new UnauthorizedAccessException();
            var session = SessionStore.GetSession(sid);
            if (session == null) throw new UnauthorizedAccessException();
            return session.UserId;
        }

        // 1. CREATE
        [HttpPost]
        public async Task<IActionResult> CreatePost([FromBody] CreatePostDto req)
        {
            try
            {
                int userId = GetUserId();

                // [Added] Fetch Real User Info for the live update
                var user = new UserQuery().GetUserById(userId);
                string authorName = user?.UserName ?? "Unknown";
                string authorPic = user?.ProfilePic ?? "/img/profile_default.jpg";

                // A. Insert Post
                var postIo = new InsertPost();
                long postId = postIo.Execute(userId, req);

                // B. Insert Attachments
                if (req.MediaAttachments != null && req.MediaAttachments.Count > 0)
                {
                    var mediaIo = new InsertPostMedia();
                    int sort = 0;
                    foreach (var item in req.MediaAttachments)
                    {
                        mediaIo.Execute(postId, item.MediaId, item.MediaType, sort++);
                    }
                }

                // C. Construct DTO for Broadcast
                var livePost = new PostDto
                {
                    Id = postId,
                    AuthorId = userId,
                    AuthorName = authorName, // [Fixed] Real Name
                    AuthorPic = authorPic,   // [Fixed] Real Pic
                    ContextType = req.ContextType,
                    ContextId = req.ContextId,
                    Type = req.Type,
                    Title = req.Title,
                    Text = req.Text,
                    ImageUrl = req.ImageUrl,
                    CreatedAt = DateTime.UtcNow,
                    CreatedAgo = "Just now",
                    Attachments = req.MediaAttachments,
                    // If we have new attachments, we might need to populate their URLs manually
                    // for the immediate UI update, OR rely on the client to guess them.
                    // For now, let's leave as is, the text/image should appear.
                    Price = req.Price,
                    LocationLabel = req.LocationLabel,
                    DifficultyLevel = req.DifficultyLevel,
                    VideoUrl = req.VideoUrl,
                    MediaId = req.MediaId,
                    Category = req.Category
                };

                // D. Broadcast
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

        // ... (GetPosts, GetSingle, GetSignalRGroupName methods remain unchanged) ...

        [HttpGet]
        public IActionResult GetPosts(
            [FromQuery] string contextType,
            [FromQuery] string contextId,
            [FromQuery] int page = 1)
        {
            try
            {
                var io = new GetPost();
                var posts = io.Execute(contextType, contextId, page);
                return Ok(posts);
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpGet("{id}")]
        public IActionResult GetSingle(long id)
        {
            try
            {
                var io = new GetPost();
                var post = io.Execute(id);
                if (post == null) return NotFound("Post not found");
                return Ok(post);
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
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
    }
}