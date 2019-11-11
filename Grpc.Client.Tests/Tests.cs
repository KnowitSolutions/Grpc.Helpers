using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Knowit.Grpc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Knowit.Grpc.Client.Tests
{
    public class Tests : ServiceTests<Echo.EchoClient, Service>
    {
        [Test]
        public async Task TestClient()
        {
            await Client.EmptyAsync(new Empty());
        }

        [Test]
        public void TestConfig()
        {
            var monitor = Services.GetRequiredService<IOptionsMonitor<GrpcClientOptions>>();
            var options = monitor.Get("Echo");
            Assert.AreEqual("echo.address",options.Address);
            Assert.AreEqual(3,options.RetryCount);
            Assert.AreEqual(1000,options.RetryInterval);
            Assert.AreEqual(true,options.RetryForever);
        }

        protected override void ConfigureAppConfiguration(IConfigurationBuilder configuration)
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string>
            {
                {"Grpc:Clients:Echo:Address", "echo.address"},
                {"Grpc:Clients:Echo:RetryCount", "3"},
                {"Grpc:Clients:Echo:RetryInterval", "1000"},
                {"Grpc:Clients:Echo:RetryForever", "true"}
            });
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);
            
            services.AddGrpc(ConfigureGrpc);
            services.AddConfigurableGrpcClient<Echo.EchoClient>(options =>
            {
                options.Address = new Uri($"http://{EndPoint}");
            });
        }
    }

    public class Service : Echo.EchoBase {
        public override Task<Empty> Empty(Empty request, ServerCallContext context) => 
            Task.FromResult(request);
    }
}
