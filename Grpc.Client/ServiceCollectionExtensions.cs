using System;
using System.Text.RegularExpressions;
using Grpc.Core;
using Grpc.Net.ClientFactory;
using Knowit.Grpc.Backoff;
using Knowit.Grpc.Correlation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Knowit.Grpc.Client
{
    public static class ServiceCollectionExtensions
    {
        private const string ConfigurationSection = "Grpc:Clients";

        public static void AddConfigurableGrpcClient<T>(
            this IServiceCollection services,
            Action<GrpcClientFactoryOptions> action)
            where T : ClientBase<T> =>
            services.AddConfigurableGrpcClient<T>(null, action);

        public static void AddConfigurableGrpcClient<T>(
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
            
            services.TryAddSingleton<Client>();
            services.AddCorrelationId();
            services.AddBackoff();
            
            services
                .AddOptions<GrpcClientOptions>(name)
                .Configure<IConfiguration>((options, config) => 
                    config.GetSection($"{ConfigurationSection}:{name}").Bind(options));


            services.AddGrpcClient<T>((provider, client) =>
            {
                provider.GetRequiredService<Client>().Configure(name, client);
                action(client);
            });
        }
    }
}
