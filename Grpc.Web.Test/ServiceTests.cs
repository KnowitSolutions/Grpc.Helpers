using System;
using System.Net;
using System.Net.Http;
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
    public class ServiceTests : ServiceTests<Echo.EchoClient, Service>
    {
        [Test]
        public void TestException()
        {
            var client = new HttpClient(new Base64Handler());
            var request = new Empty();
            var uri = $"http://{EndPoint}/Echo/Empty";

            var exception = Assert.ThrowsAsync<RpcException>(() =>
                client.PostGrpcWebAsync<Empty, Empty>(uri, request));
            Assert.AreEqual(StatusCode.Unimplemented, exception.StatusCode);
        }

        [Test]
        public async Task TestRoundtrip()
        {
            var bytes = new byte[1024 * 1024];
            var random = new Random();
            //random.NextBytes(bytes); // TODO: Uncomment

            var client = new HttpClient(new Base64Handler());
            var request = new BytesValue {Value = ByteString.CopyFrom(bytes)};
            var uri = $"http://{EndPoint}/Echo/Bytes";

            var response = await client.PostGrpcWebAsync<BytesValue, BytesValue>(uri, request);
            Assert.AreEqual(request.Value.ToByteArray(), response.Value.ToByteArray());
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
        public override Task<BytesValue> Bytes(BytesValue request, ServerCallContext context)
        {
            return Task.FromResult(request);
        }
    }
}