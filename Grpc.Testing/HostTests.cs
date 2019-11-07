using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Knowit.Grpc.Testing
{
    public class HostTests
    {
        static HostTests() =>
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        
        private IHost _host;
        private IServerAddressesFeature _addresses;
        private IServiceScope _scope;

        protected IPEndPoint EndPoint { get; private set; }
        protected IServiceProvider Services { get; private set; }

        [OneTimeSetUp]
        public async Task SetupHost()
        {
            // TODO: Check if this can be replaced by Microsoft.AspNetCore.TestHost.TestServer
            var hostBuilder = Host
                .CreateDefaultBuilder()
                .ConfigureWebHostDefaults(builder => builder
                    .Configure(Configure)
                    .ConfigureAppConfiguration(ConfigureAppConfiguration)
                    .ConfigureLogging(ConfigureLogging)
                    .ConfigureServices(ConfigureServices)
                    .ConfigureKestrel(ConfigureKestrel));

            ConfigureHost(hostBuilder);
            _host = hostBuilder.Build();
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

            var accessor = _host.Services.GetRequiredService<IHttpContextAccessor>();
            var context = new DefaultHttpContext {RequestServices = Services};
            accessor.HttpContext = context;
        }

        [TearDown]
        public void TeardownScope()
        {
            _scope.Dispose();
        }

        protected virtual void ConfigureHost(IHostBuilder host)
        {
        }

        protected virtual void Configure(IApplicationBuilder app)
        {
            _addresses = app.ServerFeatures.Get<IServerAddressesFeature>();
        }

        protected virtual void ConfigureAppConfiguration(IConfigurationBuilder configuration)
        {
        }

        private static void ConfigureLogging(ILoggingBuilder logging)
        {
            logging.SetMinimumLevel(LogLevel.Trace);
            logging.AddConsole();
        }

        protected virtual void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpContextAccessor();
        }

        protected virtual void ConfigureKestrel(KestrelServerOptions options)
        {
            options.Listen(IPAddress.Loopback, 0);
        }
    }
}