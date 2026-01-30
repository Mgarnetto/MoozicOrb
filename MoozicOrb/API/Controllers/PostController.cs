using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MoozicOrb.API.Models;
using MoozicOrb.Hubs;
using MoozicOrb.IO;
using MoozicOrb.Services; // For SessionStore & ConnectionManager
using System;
using System.Threading.Tasks;

namespace MoozicOrb.API.Controllers
{
    [ApiController]
    [Route("api/posts")]
    public class PostController : ControllerBase
    {
        private readonly IHubContext<PostHub> _hub;
        private readonly UserConnectionManager _connections;
        private readonly IHttpContextAccessor _http;

        public PostController(
            IHubContext<PostHub> hub,
            UserConnectionManager connections,
            IHttpContextAccessor http)
        {
            _hub = hub;
            _connections = connections;
            _http = http;
        }

        // --- AUTH HELPER (Consistent with UploadController) ---
        private int GetUserId()
        {
            var sid = _http.HttpContext?.Request.Headers["X-Session-Id"].ToString();
            if (string.IsNullOrEmpty(sid)) throw new UnauthorizedAccessException();

            var session = SessionStore.GetSession(sid);
            if (session == null) throw new UnauthorizedAccessException();

            return session.UserId;
        }

        // POST: Create a new post
        [HttpPost]
        public async Task<IActionResult> CreatePost([FromBody] CreatePostRequest req)
        {
            try
            {
                int userId = GetUserId(); // <--- Uses Real Auth

                // 1. Insert Post
                var postIo = new InsertPost();
                long postId = postIo.Execute(userId, req.Content, req.Type);

                // 2. Insert Media Attachments
                if (req.MediaAttachments != null)
                {
                    var mediaIo = new InsertPostMedia();
                    int sort = 0;
                    foreach (var m in req.MediaAttachments)
                    {
                        mediaIo.Execute(postId, m.MediaId, m.MediaType, sort++);
                    }
                }

                // 3. NOTIFY FOLLOWERS (Smart Iteration)
                // Assuming you have a simple GetFollowers IO class
                // var followers = new GetFollowers().Execute(userId); 
                // Placeholder until that IO exists:
                var followers = new System.Collections.Generic.List<int>();

                foreach (var followerId in followers)
                {
                    var conns = _connections.GetConnections(followerId);
                    foreach (var connId in conns)
                    {
                        await _hub.Clients.Client(connId).SendAsync("NewPost", new
                        {
                            postId = postId,
                            authorId = userId
                        });
                    }
                }

                return Ok(new { id = postId });
            }
            catch (UnauthorizedAccessException) { return Unauthorized(); }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        // GET: Fetch a single post
        [HttpGet("{id}")]
        public IActionResult GetPost(long id)
        {
            try
            {
                // Note: We might allow public access (no GetUserId check) depending on your rules.
                // If private, uncomment: int userId = GetUserId(); 

                var io = new GetPost();
                var post = io.Execute(id);

                if (post == null) return NotFound();

                return Ok(post);
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }
    }
}