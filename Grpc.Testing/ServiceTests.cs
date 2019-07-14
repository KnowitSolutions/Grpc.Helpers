using System;
using System.Collections.Generic;
using System.Linq;
using Grpc.AspNetCore.Server;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Grpc.Testing
{
    public class ServiceTests<TClient, TService> : HostTests
        where TClient : ClientBase<TClient>
        where TService : class
    {
        protected TClient Client { get; private set; }

        [SetUp]
        public void SetupClient()
        {
            Client = Services.GetService<TClient>();
        }

        protected override void Configure(IApplicationBuilder app)
        {
            base.Configure(app);
            
            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapGrpcService<TService>());
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            // TODO: Grant access to builder in configure method
            services.AddGrpc(ConfigureGrpc);
            
            // TODO: Move to extension method, possibly in service discovery
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

        protected virtual void ConfigureGrpc(GrpcServiceOptions options)
        {
        }
    }
}