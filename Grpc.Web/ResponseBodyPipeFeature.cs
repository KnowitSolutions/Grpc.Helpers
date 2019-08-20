using System.IO.Pipelines;
using Microsoft.AspNetCore.Http.Features;

namespace Knowit.Grpc.Web
{
    internal class ResponseBodyPipeFeature : IResponseBodyPipeFeature
    {
        public PipeWriter Writer { get; internal set; }
    }
}