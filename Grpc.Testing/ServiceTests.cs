using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Grpc.AspNetCore.Server;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;

namespace Grpc.Testing
{
    public class ServiceTests<TClient, TService>
        where TClient : ClientBase<TClient>
        where TService : class
    {
        private IHost _host;
        private Channel _channel;
        private IServerAddressesFeature _addresses;
        private IServiceScope _scope;
        
        protected TClient Client { get; private set; }
        protected IServiceProvider Services { get; private set; }

        [OneTimeSetUp]
        public async Task SetupHost()
        {
            _host = Host
                .CreateDefaultBuilder()
                .ConfigureWebHostDefaults(builder => builder
                    .Configure(Configure)
                    .ConfigureServices(ConfigureServices)
                    .ConfigureKestrel(ConfigureKestrel))
                .Build();
            await _host.StartAsync();
            
            var port = _addresses.Addresses.Select(address => new Uri(address).Port).First();
            var endpoint = new IPEndPoint(IPAddress.Loopback, port).ToString();
            _channel = new Channel(endpoint, ChannelCredentials.Insecure);
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
            Client = Services.GetService<TClient>();
        }

        [TearDown]
        public void TeardownScope()
        {
            _scope.Dispose();
        }

        private void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapGrpcService<TService>());
            
            _addresses = app.ServerFeatures.Get<IServerAddressesFeature>();
        }

        protected virtual void ConfigureServices(IServiceCollection services)
        {
            services.AddGrpc(ConfigureGrpc);
            
            // TODO: Move to extension method, possibly in service discovery
            services.AddScoped(provider =>
            {
                var callInvoker = provider
                    .GetService<IEnumerable<Interceptor>>()
                    .Aggregate((CallInvoker) new DefaultCallInvoker(_channel), 
                        (current, next) => current.Intercept(next));

                return (TClient) Activator.CreateInstance(typeof(TClient), callInvoker);
            });
        }

        protected virtual void ConfigureGrpc(GrpcServiceOptions options)
        {
        }

        private static void ConfigureKestrel(KestrelServerOptions options)
        {
            options.Listen(IPAddress.Loopback, 0, 
                listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
        }
    }
}