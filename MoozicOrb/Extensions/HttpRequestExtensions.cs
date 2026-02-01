using Microsoft.AspNetCore.Http;

namespace MoozicOrb.Extensions
{
    public static class HttpRequestExtensions
    {
        public static bool IsSpaRequest(this HttpRequest request)
        {
            // We will send this custom header from our JS Router
            return request.Headers["X-Spa-Request"] == "true";
        }
    }
}