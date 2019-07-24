using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Grpc.Web
{
    internal interface ITranscoder
    {
        Task TranscodeStream(RequestDelegate inner);
        Task TranscodeTrailers(IHeaderDictionary trailers);
    }
}