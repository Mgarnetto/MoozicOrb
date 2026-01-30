using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MoozicOrb.API.Models;
using MoozicOrb.IO;
using MoozicOrb.Services;
using System;

namespace MoozicOrb.API.Controllers
{
    [ApiController]
    [Route("api/collections")]
    public class CollectionsController : ControllerBase
    {
        private readonly IHttpContextAccessor _http;

        public CollectionsController(IHttpContextAccessor http)
        {
            _http = http;
        }

        // --- AUTH HELPER ---
        private int GetUserId()
        {
            var sid = _http.HttpContext?.Request.Headers["X-Session-Id"].ToString();
            if (string.IsNullOrEmpty(sid)) throw new UnauthorizedAccessException();

            var session = SessionStore.GetSession(sid);
            if (session == null) throw new UnauthorizedAccessException();

            return session.UserId;
        }

        [HttpPost("create")]
        public IActionResult Create([FromBody] CreateCollectionRequest req)
        {
            try
            {
                int userId = GetUserId(); // <--- Real Auth

                // 1. Create Header
                var io = new InsertCollection();
                long colId = io.Execute(userId, req.Title, req.Description, req.Type, req.CoverImageId);

                // 2. Add Items
                if (req.Items != null && req.Items.Count > 0)
                {
                    var itemIo = new InsertCollectionItem();
                    int sort = 0;
                    foreach (var item in req.Items)
                    {
                        itemIo.Execute(colId, item.TargetId, item.TargetType, sort++);
                    }
                }
                return Ok(new { id = colId });
            }
            catch (UnauthorizedAccessException) { return Unauthorized(); }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpGet("{id}")]
        public IActionResult Get(long id)
        {
            var io = new GetCollection();
            var data = io.Execute(id);
            if (data == null) return NotFound();
            return Ok(data);
        }
    }
}