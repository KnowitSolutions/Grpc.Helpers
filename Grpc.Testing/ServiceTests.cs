using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Grpc.AspNetCore.Server;
using Grpc.Core;
using Grpc.Core.Interceptors;
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
            services.AddGrpc(ConfigureGrpc);

            /*
            According to https://devblogs.microsoft.com/aspnet/asp-net-core-and-blazor-updates-in-net-core-3-0-preview-6/
            the following should work. Yet it doesn't. The managed implementation of gRPC client is preferable as it
            brings amongst other things deadline forwarding to the table, but for now it looks like it is broken.
            TODO: Start using the below implementation instead of the native gRPC client
            services.AddGrpcClient<TClient>((provider, options) =>
            {
                options.BaseAddress = new UriBuilder
                {
                    Scheme = "http",
                    Host = EndPoint.Address.ToString(),
                    Port = EndPoint.Port
                }.Uri;
                
                foreach (var interceptor in provider.GetService<IEnumerable<Interceptor>>())
                {
                    options.Interceptors.Add(interceptor);
                }
            });
            */
            
            services.AddScoped(provider =>
            {
                var channel = new Channel(EndPoint.ToString(), ChannelCredentials.Insecure);
                
                var callInvoker = provider
                    .GetService<IEnumerable<Interceptor>>()
                    .Aggregate((CallInvoker) new DefaultCallInvoker(channel), 
                        (current, next) => current.Intercept(next));

                return (TClient) Activator.CreateInstance(typeof(TClient), callInvoker);
            });
        }

        protected override void ConfigureKestrel(KestrelServerOptions options)
        {
            options.Listen(IPAddress.Loopback, 0,
                listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
        }

        protected virtual void ConfigureGrpc(GrpcServiceOptions options)
        {
        }
    }
}