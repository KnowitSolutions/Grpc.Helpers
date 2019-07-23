using System;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Grpc.Web
{
    internal class GrpcWebResponseEncoder
    {
        private static readonly byte[] Trailers = {0x80};

        public static async Task<long> EncodeBase64(PipeReader input, PipeWriter output)
        {
            long length = 0;
            ReadResult result;
            do
            {
                result = await input.ReadAsync();

                var unencodedSize = result.Buffer.Length - (result.IsCompleted ? 0 : result.Buffer.Length % 3);
                var encodedSize = (long) Math.Ceiling(unencodedSize / 3m) * 4; 
                var buffer = result.Buffer.Slice(0, unencodedSize);
                var bytes = new byte[unencodedSize];
                var chars = new char[encodedSize];

                var idx = 0;
                foreach (var slice in buffer)
                {
                    slice.CopyTo(bytes.AsMemory(idx, slice.Length));
                    idx += slice.Length;
                }

                length += bytes.Length;
                Convert.ToBase64CharArray(bytes, 0, bytes.Length, chars, 0);
                var data = Encoding.ASCII.GetBytes(chars);

                input.AdvanceTo(buffer.End, result.Buffer.End);
                await output.WriteAsync(data);
                await output.FlushAsync();
            } while (!result.IsCompleted);

            return length;
        }

        private static async Task EncodeMeta(PipeWriter output, byte[] type, uint length)
        {
            var size = BitConverter.GetBytes(length);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(size);
            }

            await output.WriteAsync(type);
            await output.WriteAsync(size);
        }

        public static async Task<long> EncodeTrailers(IHeaderDictionary trailers, PipeWriter output)
        {
            var trailerStrings = trailers.Select(trailer => $"{trailer.Key}: {trailer.Value}");
            var trailerBlock = string.Join("\r\n", trailerStrings) + "\r\n\r\n";
            var trailerBytes = Encoding.ASCII.GetBytes(trailerBlock);
            
            var pipe = new Pipe();
            await EncodeMeta(pipe.Writer, Trailers, (uint) trailerBytes.Length);
            await pipe.Writer.WriteAsync(trailerBytes);
            pipe.Writer.Complete();

            return await EncodeBase64(pipe.Reader, output);
        }
    }
}