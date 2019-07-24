using System;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;

namespace Grpc.Web
{
    internal class Base64Pipe
    {
        public static async Task<long> Encode(PipeReader input, PipeWriter output)
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
        
        public static async Task<long> Decode(PipeReader input, PipeWriter output)
        {
            long length = 0;
            ReadResult result;
            do
            {
                result = await input.ReadAsync();
                
                var size = result.Buffer.Length - (result.IsCompleted ? 0 : result.Buffer.Length % 4);
                var buffer = result.Buffer.Slice(0, size);
                var chars = new char[size];

                var idx = 0;
                foreach (var slice in buffer)
                {
                    Encoding.ASCII.GetChars(slice.Span, chars.AsSpan(idx, slice.Length));
                    idx += slice.Length;
                }

                for (; idx < chars.Length; idx++)
                {
                    chars[idx] = '=';
                }
                
                var bytes = Convert.FromBase64CharArray(chars, 0, chars.Length);
                length += bytes.Length;

                input.AdvanceTo(buffer.End);
                await output.WriteAsync(bytes);
                await output.FlushAsync();
            } while (!result.IsCompleted);

            return length;
        }
    }
}