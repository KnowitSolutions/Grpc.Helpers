using System;
using System.Linq;
using System.Text.RegularExpressions;
using Grpc.Core;
using Grpc.Net.ClientFactory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Knowit.Grpc.Client
{
    public static class ServiceCollectionExtensions
    {
        private const string ConfigurationSection = "Grpc:Clients";

        public static void AddGrpcClientConfiguration<T>(
            this IServiceCollection services,
            Action<GrpcClientFactoryOptions> action = null)
            where T : ClientBase<T> =>
            services.AddGrpcClientConfiguration<T>(null, action);

        public static void AddGrpcClientConfiguration<T>(
            this IServiceCollection services,
            string name = null,
            Action<GrpcClientFactoryOptions> action = null)
            where T : ClientBase<T>
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (name == null)
            {
                name = new Regex("Client$")
                    .Replace(typeof(T).Name, "");
            }

            if (action == null)
            {
                action = _ => { };
            }
            
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            
            services
                .AddOptions<GrpcClientOptions>(name)
                .Configure<IConfiguration>((options, configuration) =>
                {
                    var section = configuration.GetSection($"{ConfigurationSection}:{name}");
                    options.Address = section.Value;
                });


            services.AddGrpcClient<T>((provider, factoryOptions) =>
            {
                var monitor = provider.GetService<IOptionsMonitor<GrpcClientOptions>>();
                var clientOptions = monitor.Get(name);

                if (clientOptions.Address != null)
                {
                    var address = new Regex(@"^(?!\w+:\/\/)")
                        .Replace(clientOptions.Address, "http://");
                    factoryOptions.BaseAddress = new Uri(address);
                }

                action(factoryOptions);
            });
        }
    }
}
