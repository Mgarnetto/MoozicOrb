//using Microsoft.AspNetCore.Mvc;
//using System.Collections.Generic;
//using MoozicOrb.API.Models;
//using MoozicOrb.IO;

//namespace MoozicOrb.API.Controllers
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    public class StationsController : ControllerBase
//    {
//        // GET: api/stations
//        [HttpGet]
//        public ActionResult<List<StationDto>> GetPublicStations()
//        {
//            var io = new GetStations();
//            var stations = io.GetPublic();

//            // If none exist, we return an empty list (200 OK), not 404.
//            // This allows the frontend to show "No stations found" gracefully.
//            return Ok(stations);
//        }
//    }
//}