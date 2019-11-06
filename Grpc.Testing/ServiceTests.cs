using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Grpc.AspNetCore.Server;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.ClientFactory;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Knowit.Grpc.Testing
{
    public class ServiceTests<TClient, TService> : HostTests
        where TClient : ClientBase<TClient>
        where TService : class
    {
        static ServiceTests() =>
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

        protected TClient Client { get; private set; }

        [SetUp]
        public void SetupClient()
        {
            Client = Services.GetRequiredService<TClient>();
        }

        protected override void Configure(IApplicationBuilder app)
        {
            base.Configure(app);

            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapGrpcService<TService>());
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);
            
            services.AddGrpc(ConfigureGrpc);
            var clientBuilder = services.AddGrpcClient<TClient>(options =>
            {
                options.Address = new UriBuilder
                {
                    Host = EndPoint.Address.ToString(),
                    Port = EndPoint.Port
                }.Uri;

                ConfigureGrpcClient(options);
            });
            ConfigureGrpcClient(clientBuilder);
        }

        protected override void ConfigureKestrel(KestrelServerOptions options)
        {
            options.Listen(IPAddress.Loopback, 0,
                listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
        }

        protected virtual void ConfigureGrpc(GrpcServiceOptions options)
        {
        }

        protected virtual void ConfigureGrpcClient(GrpcClientFactoryOptions options)
        {
        }

        protected virtual void ConfigureGrpcClient(IHttpClientBuilder client)
        {
        }
    }
}