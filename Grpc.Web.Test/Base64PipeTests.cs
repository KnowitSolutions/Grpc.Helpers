using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Knowit.Grpc.Web.Tests
{
    public class Base64PipeTests
    {
        private static readonly Random Random = new Random();
        
        [Test]
        [Repeat(10)]
        public async Task Roundtrip()
        {
            var pipe1 = new Pipe();
            var pipe2 = new Pipe();
            var pipe3 = new Pipe();
            
            var writeTask = Write(pipe1.Writer);
            var encodeTask = Base64Pipe.Encode(pipe1.Reader, pipe2.Writer);
            var decodeTask = Base64Pipe.Decode(pipe2.Reader, pipe3.Writer);
            var readTask = Read(pipe3.Reader);

            var input = await writeTask;
            pipe1.Writer.Complete();
            await encodeTask;
            pipe1.Reader.Complete();
            pipe2.Writer.Complete();
            await decodeTask;
            pipe2.Reader.Complete();
            pipe3.Writer.Complete();
            var output = await readTask;
            pipe3.Reader.Complete();

            Assert.AreEqual(input, output);
        }

        private static async Task<byte[]> Write(PipeWriter output)
        {
            var count = Random.Next(1, 1024);
            var input = new byte[count];
            Random.NextBytes(input);
            
            var memory = output.GetMemory(count);
            input.CopyTo(memory);
            output.Advance(count);
            await output.FlushAsync();

            return input;
        }

        private static async Task<byte[]> Read(PipeReader input)
        {
            var data = new MemoryStream();
            
            ReadResult result;
            do
            {
                result = await input.ReadAsync();
                foreach (var slice in result.Buffer)
                {
                    await data.WriteAsync(slice);
                }
                input.AdvanceTo(result.Buffer.End);
            } while (!result.IsCompleted);

            return data.ToArray();
        }
    }
}