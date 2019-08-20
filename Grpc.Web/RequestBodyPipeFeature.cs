using System.IO.Pipelines;
using Microsoft.AspNetCore.Http.Features;

namespace Knowit.Grpc.Web
{
    internal class RequestBodyPipeFeature : IRequestBodyPipeFeature
    {
        public PipeReader Reader { get; internal set; }
    }
}