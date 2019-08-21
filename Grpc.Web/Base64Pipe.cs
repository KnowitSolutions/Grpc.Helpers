using System.IO.Pipelines;
using System.Threading.Tasks;

namespace Knowit.Grpc.Web
{
    internal class Base64Pipe
    {
        private static readonly Base64Encoder Base64Encoder = new Base64Encoder();
        private static readonly Base64Decoder Base64Decoder = new Base64Decoder();

        public static Task<long> Encode(PipeReader input, PipeWriter output) => Transcode(input, output, Base64Encoder);

        public static Task<long> Decode(PipeReader input, PipeWriter output) => Transcode(input, output, Base64Decoder);

        private static async Task<long> Transcode(PipeReader input, PipeWriter output, Base64 base64)
        {
            byte @byte = 0x00;
            var state = 0;
            long length = 0;

            ReadResult result;
            do
            {
                result = await input.ReadAsync();
                var outputMemory = output.GetMemory((int) base64.RequiredBufferSize(result.Buffer.Length));
                
                long written = 0;
                outputMemory.Span[0] = @byte;
                
                foreach (var inputMemory in result.Buffer)
                {
                    base64.ProcessBlock(inputMemory, outputMemory, ref written, ref state);
                }

                var idx = (int) written;
                @byte = outputMemory.Span[idx];
                length += written;

                input.AdvanceTo(result.Buffer.End);
                output.Advance((int) written);
                await output.FlushAsync();
            } while (!result.IsCompleted);

            {
                var outputMemory = output.GetMemory(base64.OutputBlockSize);
                outputMemory.Span[0] = @byte;
                
                var written = base64.Finalize(outputMemory, 0, state);
                output.Advance((int) written);
                await output.FlushAsync();

                length += written;
            }

            return length;
        }
    }
}