using System;
using System.IO;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Knowit.Grpc.Web
{
    internal class BodyRedirector : IDisposable
    {
        private readonly HttpContext _context;
        private readonly Stream _requestBody;
        private readonly IRequestBodyPipeFeature _requestBodyPipe;
        private readonly IHttpResponseBodyFeature _responseBody;

        public BodyRedirector(HttpContext context, Pipe requestPipe, Pipe responsePipe)
        {
            // gRPC middleware don't flush so we have to flush for them
            // This means we need access to their pipe writer, so we switch the body pipe features as well to get raw access
            // Although it seems like we could instead switch the body pipe properties on the request and the response
            // as documented by https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware/request-response?view=aspnetcore-3.0#adapters
            // this ability was removed in https://github.com/aspnet/AspNetCore/pull/10154
            _context = context;

            _requestBody = _context.Request.Body;
            _context.Request.Body = requestPipe.Reader.AsStream(true);
            _requestBodyPipe = _context.Features.Get<IRequestBodyPipeFeature>();
            _context.Features.Set<IRequestBodyPipeFeature>(
                new RequestBodyPipeFeature {Reader = requestPipe.Reader});

            _responseBody = _context.Features.Get<IHttpResponseBodyFeature>();
            var responseBody = new ResponseBodyFeature(_responseBody) {Writer = responsePipe.Writer};
            _context.Features.Set<IHttpResponseBodyFeature>(responseBody);
        }

        public void Dispose()
        {
            _context.Request.Body.Dispose();
            _context.Request.Body = _requestBody;
            _context.Features.Set(_requestBodyPipe);
            _context.Features.Set(_responseBody);
        }
    }
}