using Microsoft.AspNetCore.Mvc;
using MoozicOrb.Radio;
using System.Threading.Tasks;

namespace MoozicOrb.API.Controllers
{
    [ApiController]
    [Route("api/webrtc")]
    public class WebRtcController : ControllerBase
    {
        public WebRtcController()
        {
            RadioEngine.Start();
        }

        [HttpGet("offer")]
        public async Task<string> GetOffer()
        {
            // Return SDP offer to client
            return await Task.FromResult(RadioEngine.WebRtc.CreateOffer());
        }

        [HttpPost("answer")]
        public async Task<IActionResult> SetAnswer([FromBody] string sdp)
        {
            RadioEngine.WebRtc.SetAnswer(sdp);
            return await Task.FromResult(Ok());
        }
    }
}


