using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Testing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Grpc.Web.Test
{
    public class Tests : ServiceTests<Echo.EchoClient, Service>
    {
        // TODO: Unit test
        // TODO: Exception handling
        // TODO: Refactor client
        
        [Test]
        public async Task TestEmpty()
        {
            await Invoke<Empty, Empty>(new Empty(), "Echo", "Empty");
        }

        [Test]
        public async Task TestBytes()
        {
            var bytes = new byte[1024 * 1024];
            var random = new Random();
            random.NextBytes(bytes);

            var request = new BytesValue {Value = ByteString.CopyFrom(bytes)};
            var response = await Invoke<BytesValue, BytesValue>(request, "Echo", "Bytes");

            Assert.AreEqual(request.Value, response.Value);
        }

        private async Task<TResponse> Invoke<TRequest, TResponse>(TRequest requestMessage, string service,
            string method)
            where TRequest : IMessage<TRequest>
            where TResponse : IMessage<TResponse>, new()
        {
            var request = EncodeMessage(requestMessage);
            var client = new HttpClient();
            var content = new StringContent(request, Encoding.ASCII, "application/grpc-web-text+protobuf");
            var response = await client.PostAsync($"http://{EndPoint}/{service}/{method}", content);

            Assert.IsTrue(response.IsSuccessStatusCode);
            Assert.That(
                response.Headers.ToDictionary(x => x.Key, x => x.Value), 
                !Contains.Key("grpc-status"));

            var body = await response.Content.ReadAsStringAsync();
            var (responseMessage, trailers) = DecodeMessage<TResponse>(body);

            Assert.That(trailers, Contains.Key("grpc-status"));
            Assert.AreEqual("0", trailers["grpc-status"]);

            return responseMessage;
        }

        private static string EncodeMessage<T>(T message) where T : IMessage<T>
        {
            var bytes = message.ToByteArray();

            var length = BitConverter.GetBytes((uint) bytes.Length);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(length);
            }

            var data = new[] {new byte[] {0x00}, length, bytes}
                .SelectMany(x => x)
                .ToArray();

            return Convert.ToBase64String(data);
        }

        private static (T, IDictionary<string, string>) DecodeMessage<T>(string data)
            where T : IMessage<T>, new()
        {
            var parts = DecodeEnvelope(data).ToList();

            Assert.AreEqual(2, parts.Count);
            Assert.IsFalse(parts[0].Item1);
            Assert.IsFalse(parts[0].Item2);
            Assert.IsFalse(parts[1].Item1);
            Assert.IsTrue(parts[1].Item2);

            var message = new T();
            message.MergeFrom(parts[0].Item3);

            var trailers = Encoding.ASCII
                .GetString(parts[1].Item3)
                .Trim()
                .Split("\r\n")
                .Select(trailer => trailer.Split(":"))
                .ToDictionary(trailer => trailer[0].Trim(), trailer => trailer[1].Trim());

            return (message, trailers);
        }

        private static IEnumerable<(bool, bool, byte[])> DecodeEnvelope(string data)
        {
            while (data.Length > 0)
            {
                var bytes = Convert.FromBase64String(data.Substring(0, 8));

                var isCompressed = Convert.ToBoolean(bytes[0] & 0x01);
                var isTrailers = Convert.ToBoolean(bytes[0] & 0x80);
                if (BitConverter.IsLittleEndian) Array.Reverse(bytes, 1, 4);
                var length = BitConverter.ToUInt32(bytes[1..5]);

                var size = (int) Math.Ceiling((length + 5) / 3m) * 4;
                bytes = Convert.FromBase64String(data.Substring(0, size));

                yield return (isCompressed, isTrailers, bytes[5..]);

                data = data.Substring(size);
            }
        }

        protected override void Configure(IApplicationBuilder app)
        {
            app.UseGrpcWeb();
            base.Configure(app);
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);
            services.AddGrpcWeb();
        }

        protected override void ConfigureKestrel(KestrelServerOptions options)
        {
            options.Listen(IPAddress.Loopback, 0);
        }
    }

    public class Service : Echo.EchoBase
    {
        public override Task<Empty> Empty(Empty request, ServerCallContext context)
        {
            return Task.FromResult(new Empty());
        }

        public override Task<BytesValue> Bytes(BytesValue request, ServerCallContext context)
        {
            return Task.FromResult(request);
        }
    }
}