using System.Collections.Generic;
using Grpc.Net.ClientFactory;
using Grpc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Grpc.Client.Tests
{
    public class Tests : HostTests
    {
        [Test]
        public void TestClientExists()
        {
            Services.GetRequiredService<Echo.EchoClient>();
        }

        [TestCase("EchoClient", ExpectedResult = "echo.address")]
        [TestCase("EchoClient, Named", ExpectedResult = "named.address")]
        [TestCase("EchoClient, Structured", ExpectedResult = "structured.address")]
        public string TestClientAddress(string name)
        {
            var snapshot = Services.GetService<IOptionsSnapshot<GrpcClientFactoryOptions>>();
            var options = snapshot.Get(name);
            return options.BaseAddress.Host;
        }

        protected override void ConfigureAppConfiguration(IConfigurationBuilder configuration)
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string>
            {
                {"Grpc:Clients:Echo", "echo.address"},
                {"Grpc:Clients:Named", "named.address"},
                {"Grpc:Clients:Structured:Address", "structured.address"}
            });
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);
            services.AddGrpcClientConfiguration<Echo.EchoClient>();
            services.AddGrpcClientConfiguration<Echo.EchoClient>("Named");
            services.AddGrpcClientConfiguration<Echo.EchoClient>("Structured");
        }
    }
}