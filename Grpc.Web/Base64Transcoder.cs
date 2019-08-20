using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Knowit.Grpc.Web
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
            var requestReader = _httpContextAccessor.HttpContext.Request.BodyReader;
            var responseWriter = _httpContextAccessor.HttpContext.Response.BodyWriter;
            var requestPipe = new Pipe();
            var responsePipe = new Pipe();
            var decode = Base64Pipe.Decode(requestReader, requestPipe.Writer);
            var encode = Base64Pipe.Encode(responsePipe.Reader, responseWriter);
            
            using (new BodyRedirector(_httpContextAccessor.HttpContext, requestPipe, responsePipe))
            {
                var next = inner(_httpContextAccessor.HttpContext);
                
                var length = await decode;
                requestPipe.Writer.Complete();
                _logger.LogTrace("Decoded {Length} bytes from base64", length);
                
                await next;
                await responsePipe.Writer.FlushAsync(); // TODO: May be unnecessary
                responsePipe.Writer.Complete();
            }

            {
                var length = await encode;
                responsePipe.Reader.Complete();
                _logger.LogTrace("Encoded {Length} bytes to base64", length);
            }
            
            await encode;
        }

        public async Task TranscodeTrailers(IHeaderDictionary trailers)
        {
            var pipe = new Pipe();
            var stream = GrpcWebTrailers.Stream(trailers, pipe.Writer);
            var encode = Base64Pipe.Encode(pipe.Reader, _httpContextAccessor.HttpContext.Response.BodyWriter);

            await stream;
            pipe.Writer.Complete();
            var length = await encode;
            pipe.Reader.Complete();
            _logger.LogTrace("Encoded {Length} bytes to base64", length);
        }
    }
}