using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;

namespace Knowit.Grpc.Web
{
    internal class ResponseBodyFeature : IHttpResponseBodyFeature
    {
        private readonly IHttpResponseBodyFeature _priorFeature;
        
        public ResponseBodyFeature(IHttpResponseBodyFeature priorFeature)
        {
            _priorFeature = priorFeature;
        }
        
        public Stream Stream => null;
        public PipeWriter Writer { get; internal set; }

        public async Task StartAsync(CancellationToken cancellationToken = new CancellationToken()) =>
            await _priorFeature.StartAsync(cancellationToken);

        public async Task CompleteAsync() => 
            await _priorFeature.CompleteAsync();

        public Task SendFileAsync(string path, long offset, long? count,
            CancellationToken cancellationToken = new CancellationToken()) =>
            throw new NotImplementedException();

        public void DisableBuffering() =>
            throw new NotImplementedException();
    }
}