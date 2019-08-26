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
        public Stream Stream => null;
        public PipeWriter Writer { get; internal set; }

        public Task StartAsync(CancellationToken cancellationToken = new CancellationToken()) =>
            Task.CompletedTask;

        public Task CompleteAsync() =>
            Task.CompletedTask;

        public Task SendFileAsync(string path, long offset, long? count,
            CancellationToken cancellationToken = new CancellationToken()) =>
            throw new NotImplementedException();

        public void DisableBuffering() =>
            throw new NotImplementedException();
    }
}