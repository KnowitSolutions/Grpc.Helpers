using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Knowit.Grpc.Testing;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using NUnit.Framework;

namespace Knowit.Kestrel.ProtocolMultiplexing.Tests
{
    public class Tests : HostTests
    {
        [OneTimeSetUp]
        public void EnableHttp2()
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        }
        
        [Test]
        [TestCase(1, 1)]
        [TestCase(2, 0)]
        public async Task TestHttp(int httpMajor, int httpMinor)
        {
            using var client = new HttpClient();
            using var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"http://{EndPoint}"),
                Version = new Version(httpMajor, httpMinor)
            };
            using var response = await client.SendAsync(request);
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        protected override void ConfigureKestrel(KestrelServerOptions options)
        {
            options.Listen(IPAddress.Loopback, 0, listenOptions => listenOptions.UseProtocolMultiplexing());
        }
    }
}