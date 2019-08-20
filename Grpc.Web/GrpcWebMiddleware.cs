using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Grpc.Web
{
    internal class GrpcWebMiddleware : IHttpResponseTrailersFeature
    {
        private static readonly Regex ContentType = new Regex(
            @"application/grpc-web(?:-(?<text>text))?(?:\+(?<format>\w+))?");

        private readonly RequestDelegate _next;
        private readonly ILogger<GrpcWebMiddleware> _logger;
        private readonly BinaryTranscoder _binaryTranscoder;
        private readonly Base64Transcoder _base64Transcoder;

        public IHeaderDictionary Trailers { get; set; } = new HeaderDictionary();

        public GrpcWebMiddleware(
            RequestDelegate next,
            ILogger<GrpcWebMiddleware> logger,
            BinaryTranscoder binaryTranscoder,
            Base64Transcoder base64Transcoder)
        {
            _next = next;
            _logger = logger;
            _binaryTranscoder = binaryTranscoder;
            _base64Transcoder = base64Transcoder;
        }

        public async Task Invoke(HttpContext context)
        {
            var match = ContentType.Match(context.Request.ContentType ?? "");
            if (match.Success)
            {
                _logger.LogInformation("Intercepted gRPC Web request to {Uri}", context.Request.Path.Value);
                
                var isText = match.Groups["text"].Success;
                var format = match.Groups["format"].Success ? match.Groups["format"].Value : null;
                var transcoder = isText ? (ITranscoder) _base64Transcoder : _binaryTranscoder;
                
                await Intercept(context, isText, format, transcoder);
            }
            else
            {
                await _next(context);
            }
        }

        private async Task Intercept(HttpContext context, bool isText, string format, ITranscoder transcoder)
        {
            var textPostfix = isText ? "-text" : "";
            var formatPostfix = format != null ? $"+{format}" : "";
            
            context.Features.Set<IHttpResponseTrailersFeature>(this);
            context.Request.ContentType = $"application/grpc{formatPostfix}";
            context.Response.OnStarting(() =>
            {
                context.Response.ContentType = $"application/grpc-web{textPostfix}{formatPostfix}";
                return Task.CompletedTask;
            });

            await transcoder.TranscodeStream(_next);
            if (Trailers.Count > 0)
            {
                _logger.LogDebug("Adding trailers");
                await transcoder.TranscodeTrailers(Trailers);
            }
            else
            {
                _logger.LogDebug("Skipping trailers");
            }
        }
    }
}