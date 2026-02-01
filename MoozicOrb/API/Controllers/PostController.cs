using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MoozicOrb.API.Models;      // For PostDto
using MoozicOrb.Hubs;
using MoozicOrb.IO;
using MoozicOrb.Services;        // For SessionStore
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

        public PostController(IHubContext<PostHub> hub, IHttpContextAccessor http)
        {
            _hub = hub;
            _http = http;
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

                var postIo = new InsertPost();
                long postId = postIo.Execute(userId, req);

                // Construct DTO for Broadcast (Manual Mapping for Speed)
                var livePost = new PostDto
                {
                    Id = postId,
                    AuthorId = userId,
                    AuthorName = "Me", // In prod, fetch user name from session cache
                    AuthorPic = "/img/default.png",
                    ContextType = req.ContextType,
                    ContextId = req.ContextId,
                    Type = req.Type,
                    Title = req.Title,
                    Text = req.Text,
                    ImageUrl = req.ImageUrl,
                    CreatedAt = DateTime.UtcNow,
                    CreatedAgo = "Just now",
                    Price = req.Price,
                    LocationLabel = req.LocationLabel,
                    DifficultyLevel = req.DifficultyLevel,
                    VideoUrl = req.VideoUrl,
                    MediaId = req.MediaId,
                    Category = req.Category
                };

                // Broadcast
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

        // 2. GET FEED (List)
        [HttpGet]
        public IActionResult GetPosts(
            [FromQuery] string contextType,
            [FromQuery] string contextId,
            [FromQuery] int page = 1)
        {
            try
            {
                // Uses Method Overload B
                var io = new GetPost();
                var posts = io.Execute(contextType, contextId, page);
                return Ok(posts);
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        // 3. GET SINGLE (By ID)
        [HttpGet("{id}")]
        public IActionResult GetSingle(long id)
        {
            try
            {
                // Uses Method Overload A
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