using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;

namespace Grpc.Testing
{
    public class HostTests
    {
        private IHost _host;
        private IServerAddressesFeature _addresses;
        private IServiceScope _scope;
        
        protected IPEndPoint EndPoint { get; private set; }
        protected IServiceProvider Services { get; private set; }

        [OneTimeSetUp]
        public async Task SetupHost()
        {
            _host = Host
                .CreateDefaultBuilder()
                .ConfigureWebHostDefaults(builder => builder
                    .Configure(Configure)
                    .ConfigureAppConfiguration(ConfigureAppConfiguration)
                    .ConfigureServices(ConfigureServices)
                    .ConfigureKestrel(ConfigureKestrel))
                .Build();
            await _host.StartAsync();
            
            var port = _addresses.Addresses.Select(address => new Uri(address).Port).First();
            EndPoint = new IPEndPoint(IPAddress.Loopback, port);
        }

        [OneTimeTearDown]
        public async Task TeardownHost()
        {
            await _host.StopAsync();
        }

        [SetUp]
        public void SetupScope()
        {
            _scope = _host.Services.CreateScope();
            Services = _scope.ServiceProvider;
        }

        [TearDown]
        public void TeardownScope()
        {
            _scope.Dispose();
        }

        protected virtual void Configure(IApplicationBuilder app)
        {
            _addresses = app.ServerFeatures.Get<IServerAddressesFeature>();
        }

        protected virtual void ConfigureAppConfiguration(IConfigurationBuilder configuration)
        {
        }

        protected virtual void ConfigureServices(IServiceCollection services)
        {
        }

        private static void ConfigureKestrel(KestrelServerOptions options)
        {
            options.Listen(IPAddress.Loopback, 0, 
                listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
        }
    }
}