using System;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Knowit.Grpc.Web
{
    internal class GrpcWebTrailers
    {
        private static readonly byte[] Trailers = {0x80};

        private static async Task StreamMeta(PipeWriter output, byte[] type, uint length)
        {
            var size = BitConverter.GetBytes(length);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(size);
            }

            await output.WriteAsync(type);
            await output.WriteAsync(size);
        }

        public static async Task Stream(IHeaderDictionary trailers, PipeWriter output)
        {
            var trailerStrings = trailers.Select(trailer => $"{trailer.Key}: {trailer.Value}");
            var trailerBlock = string.Join("\r\n", trailerStrings) + "\r\n\r\n";
            var trailerBytes = Encoding.ASCII.GetBytes(trailerBlock);
            
            await StreamMeta(output, Trailers, (uint) trailerBytes.Length);
            await output.WriteAsync(trailerBytes);
        }
    }
}