using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Knowit.Grpc.Web
{
    internal interface ITranscoder
    {
        Task TranscodeStream(RequestDelegate inner);
        Task TranscodeTrailers(IHeaderDictionary trailers);
    }
}