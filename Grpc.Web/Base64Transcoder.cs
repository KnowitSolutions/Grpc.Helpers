using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grpc.Web
{
    internal class Base64Transcoder : ITranscoder
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<Base64Transcoder> _logger;
        
        public Base64Transcoder(IHttpContextAccessor httpContextAccessor, ILogger<Base64Transcoder> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task TranscodeStream(RequestDelegate inner)
        {
            var redirector = _httpContextAccessor.HttpContext.RequestServices.GetRequiredService<BodyRedirector>();
            using (redirector)
            {
                var decode = Decode(redirector.RequestReader, redirector.RequestWriter);
                var next = inner(_httpContextAccessor.HttpContext);
                var encode = Encode(redirector.ResponseReader, redirector.ResponseWriter);
                
                await decode;
                redirector.RequestWriter.Complete();
                await next;
                _httpContextAccessor.HttpContext.Response.BodyWriter.Complete();
                await encode;
                redirector.ResponseReader.Complete();
            }
        }

        public async Task TranscodeTrailers(IHeaderDictionary trailers)
        {
            var pipe = new Pipe();
            var stream = GrpcWebTrailers.Stream(trailers, pipe.Writer);
            var encode = Encode(pipe.Reader, _httpContextAccessor.HttpContext.Response.BodyWriter);

            await stream;
            pipe.Writer.Complete();
            await encode;
            pipe.Reader.Complete();
        }

        private async Task Decode(PipeReader input, PipeWriter output)
        {
            var length = await Base64Pipe.Decode(input, output);
            _logger.LogTrace("Decoded {Length} bytes from base64", length);
        }

        private async Task Encode(PipeReader input, PipeWriter output)
        {
            var length = await Base64Pipe.Encode(input, output);
            _logger.LogTrace("Encoded {Length} bytes to base64", length);
        }
    }
}