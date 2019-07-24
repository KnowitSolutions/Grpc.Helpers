using System;
using System.IO;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Http;

namespace Grpc.Web
{
    internal class BodyRedirector : IDisposable
    {
        public PipeReader RequestReader { get; private set; }
        public PipeWriter RequestWriter => _requestPipe.Writer;
        public PipeReader ResponseReader => _responsePipe.Reader;
        public PipeWriter ResponseWriter { get; private set; }
        
        private readonly IHttpContextAccessor _contextAccessor;
        
        private readonly Pipe _requestPipe = new Pipe();
        private readonly Pipe _responsePipe = new Pipe();
        private Stream _originalRequestBody;
        private Stream _originalResponseBody;

        public BodyRedirector(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
            Start();
        }
        
        public void Dispose() => End();

        private void Start()
        {
            RequestReader = _contextAccessor.HttpContext.Request.BodyReader;
            ResponseWriter = _contextAccessor.HttpContext.Response.BodyWriter;
            
            _originalRequestBody = _contextAccessor.HttpContext.Response.Body;
            _originalResponseBody = _contextAccessor.HttpContext.Response.Body;
            
            _contextAccessor.HttpContext.Request.Body = _requestPipe.Reader.AsStream();
            _contextAccessor.HttpContext.Response.Body = _responsePipe.Writer.AsStream();
        }

        private void End()
        {
            _contextAccessor.HttpContext.Request.Body = _originalRequestBody;
            _contextAccessor.HttpContext.Response.Body = _originalResponseBody;
        }

    }
}