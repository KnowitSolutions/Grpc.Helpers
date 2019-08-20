using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Knowit.Grpc.Testing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Knowit.Grpc.Web.Tests
{
    public class GrpcWebMiddlewareTests : HostTests
    {
        private static readonly Random Random = new Random();

        private DefaultHttpContext _context;
        private GrpcWebMiddleware _middleware;
        private Pipe _inputPipe;
        private Pipe _outputPipe;
        private IDisposable _redirector;

        [SetUp]
        public void Setup()
        {
            var input = new Pipe();
            _inputPipe = input;
            var output = new Pipe();
            _outputPipe = output;

            _context = new DefaultHttpContext {RequestServices = Services};
            _context.Request.Path = "/Service/Method";
            Services.GetRequiredService<IHttpContextAccessor>().HttpContext = _context;
            _redirector = new BodyRedirector(_context,input, output);

            var logger = Services.GetRequiredService<ILogger<GrpcWebMiddleware>>();
            var base64Transcoder = Services.GetRequiredService<Base64Transcoder>();
            var binaryTranscoder = Services.GetRequiredService<BinaryTranscoder>();
            _middleware = new GrpcWebMiddleware(Forward, logger, binaryTranscoder, base64Transcoder);
        }

        [TearDown]
        public void Teardown()
        {
            _redirector.Dispose();
        }

        [Test]
        public async Task TestBinary()
        {
            _context.Request.ContentType = "application/grpc-web+protobuf";

            var writeTask = Write(_inputPipe.Writer);
            var middlewareTask = _middleware.Invoke(_context);
            var readTask = Read(_outputPipe.Reader);

            var input = await writeTask;
            _inputPipe.Writer.Complete();
            await middlewareTask;
            _inputPipe.Reader.Complete();
            _outputPipe.Writer.Complete();
            var output = await readTask;
            _outputPipe.Reader.Complete();

            Assert.AreEqual(input, output[..input.Length]);
        }

        [Test]
        public async Task TestBase64()
        {
            _context.Request.ContentType = "application/grpc-web-text+protobuf";

            var encodePipe = new Pipe();
            var decodePipe = new Pipe();

            var writeTask = Write(encodePipe.Writer);
            var encodeTask = Base64Pipe.Encode(encodePipe.Reader, _inputPipe.Writer);
            var middlewareTask = _middleware.Invoke(_context);
            var decodeTask = Base64Pipe.Decode(_outputPipe.Reader, decodePipe.Writer);
            var readTask = Read(decodePipe.Reader);

            var input = await writeTask;
            encodePipe.Writer.Complete();
            await encodeTask;
            encodePipe.Reader.Complete();
            _inputPipe.Writer.Complete();
            await middlewareTask;
            _inputPipe.Reader.Complete();
            _outputPipe.Writer.Complete();
            await decodeTask;
            _outputPipe.Reader.Complete();
            decodePipe.Writer.Complete();
            var output = await readTask;
            decodePipe.Reader.Complete();

            Assert.AreEqual(input, output[..input.Length]);
        }

        [Test]
        public async Task TestTrailers()
        {
            _context.Request.ContentType = "application/grpc-web+protobuf";

            var middlewareTask = _middleware.Invoke(_context);
            var readTask = Read(_outputPipe.Reader);

            _inputPipe.Writer.Complete();
            await middlewareTask;
            _inputPipe.Reader.Complete();
            _outputPipe.Writer.Complete();
            var output = await readTask;
            _outputPipe.Reader.Complete();

            var trailers = Encoding.ASCII
                .GetString(output[5..])
                .TrimEnd()
                .Split("\r\n")
                .Select(trailer => trailer.Split(": "))
                .ToDictionary(trailer => trailer[0], trailer => trailer[1]);
            var correct = new Dictionary<string, string> {{"key", "value"}};
            Assert.AreEqual(trailers, correct);
        }

        private static async Task<byte[]> Write(PipeWriter output)
        {
            var count = Random.Next(1, 1024 * 1024);
            var input = new byte[count];
            Random.NextBytes(input);

            var memory = output.GetMemory(count);
            input.CopyTo(memory);
            output.Advance(count);
            await output.FlushAsync();

            return input;
        }

        private static async Task Forward(HttpContext context)
        {
            var trailers = context.Features.Get<IHttpResponseTrailersFeature>().Trailers;
            trailers.Add("key", "value");

            ReadResult result;
            do
            {
                result = await context.Request.BodyReader.ReadAsync();
                var memory = context.Response.BodyWriter.GetMemory((int) result.Buffer.Length);

                foreach (var slice in result.Buffer)
                {
                    slice.CopyTo(memory);
                    memory = memory.Slice(slice.Length);
                }
                
                context.Request.BodyReader.AdvanceTo(result.Buffer.End);
                context.Response.BodyWriter.Advance((int) result.Buffer.Length);
                await context.Response.BodyWriter.FlushAsync();
            } while (!result.IsCompleted);
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

        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);
            services.AddGrpcWeb();
        }
    }
}