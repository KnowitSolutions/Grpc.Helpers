using System.IO.Pipelines;
using System.Net;
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

        public IHeaderDictionary Trailers { get; set; } = new HeaderDictionary();

        public GrpcWebMiddleware(RequestDelegate next, ILogger<GrpcWebMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            var match = ContentType.Match(context.Request.ContentType);
            if (match.Success)
            {
                _logger.LogInformation("Intercepted gRPC Web request to {Uri}", context.Request.Path.Value);

                var format = match.Groups["format"].Success ? match.Groups["format"].Value : null;
                var isText = match.Groups["text"].Success;

                if (isText)
                {
                    await Intercept(context, format);
                }
                else
                {
                    context.Response.StatusCode = (int) HttpStatusCode.NotImplemented;
                    _logger.LogWarning(
                        "Unimplemented: Rejecting binary encoded gRPC Web request to {Uri}",
                        context.Request.Path.Value);
                }
            }
            else
            {
                await _next(context);
            }
        }

        private async Task Intercept(HttpContext context, string format)
        {
            var requestPipe = new Pipe();
            var responsePipe = new Pipe();

            context.Features.Set<IHttpResponseTrailersFeature>(this);
            context.Request.ContentType = "application/grpc" + (format != null ? $"+{format}" : "");
            var requestReader = context.Request.BodyReader;
            context.Request.Body = requestPipe.Reader.AsStream();
            var responseWriter = context.Response.BodyWriter;
            var responseBody = context.Response.Body;
            context.Response.Body = responsePipe.Writer.AsStream();

            var decode = GrpcWebRequestDecoder.DecodeBase64(requestReader, requestPipe.Writer);
            var next = _next(context);
            var encode = GrpcWebResponseEncoder.EncodeBase64(responsePipe.Reader, responseWriter);

            var requestLength = await decode;
            _logger.LogTrace("Decoded request with {Length} bytes", requestLength);

            requestPipe.Writer.Complete();
            await next;
            responsePipe.Writer.Complete();

            var responseLength = await encode;
            _logger.LogTrace("Encoded response with {Length} bytes", responseLength);

            context.Response.Body = responseBody;
            if (Trailers.Count > 0)
            {
                _logger.LogDebug("Adding trailers");
                var trailersLength = await GrpcWebResponseEncoder.EncodeTrailers(Trailers, context.Response.BodyWriter);
                _logger.LogTrace("Encoded trailers with {Length} bytes", trailersLength);
            }
            else
            {
                _logger.LogDebug("Skipping trailers");
            }
        }
    }
}