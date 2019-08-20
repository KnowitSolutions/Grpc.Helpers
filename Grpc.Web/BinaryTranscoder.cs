using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Knowit.Grpc.Web
{
    internal class BinaryTranscoder : ITranscoder
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        
        public BinaryTranscoder(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        
        public async Task TranscodeStream(RequestDelegate inner)
        {
            await inner(_httpContextAccessor.HttpContext);
        }

        public async Task TranscodeTrailers(IHeaderDictionary trailers)
        {
            await GrpcWebTrailers.Stream(trailers, _httpContextAccessor.HttpContext.Response.BodyWriter);
        }
    }
}